using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;
using NewVivaApi.Data;
using NewVivaApi.Models;
using NewVivaApi.Services;
using AutoMapper;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using NewVivaApi.Extensions;

namespace NewVivaApi.Controllers.OData
{
    [Authorize]
    public class PayAppPaymentsController : ODataController
    {
        private readonly AppDbContext _context;
        //private readonly ODataValidationSettings _validationSettings = new ODataValidationSettings();
        private readonly IMapper _mapper;
        private readonly ILogger<PayAppPaymentsController> _logger;
        private readonly FinancialSecurityService _financialSecurityService;

        public PayAppPaymentsController(
            AppDbContext context,
            IMapper mapper,
            ILogger<PayAppPaymentsController> logger,
            FinancialSecurityService financialSecurityService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _financialSecurityService = financialSecurityService;
        }

        private IQueryable<PayAppPaymentsVw> GetSecureModel()
        {
            IQueryable<PayAppPaymentsVw> model;

            if (User.Identity.IsServiceUser())
            {
                return null;
            }

            if (User.Identity.IsVivaUser())
            {
                model = _context.PayAppPaymentsVws;
            }
            else if (User.Identity.IsGeneralContractor())
            {
                int? currentGeneralContractorId = User.Identity.GetGeneralContractorId();
                model =
                    from payApp in _context.PayAppsVws
                    join generalContractorPayments in _context.PayAppPaymentsVws on payApp.PayAppId equals generalContractorPayments.PayAppId
                    where payApp.GeneralContractorId == currentGeneralContractorId
                    select generalContractorPayments;
            }
            else if (User.Identity.IsSubContractor())
            {
                int? currentSubcontractorId = User.Identity.GetSubcontractorId();
                model =
                    from payApp in _context.PayAppsVws
                    join subcontractorPayments in _context.PayAppPaymentsVws on payApp.PayAppId equals subcontractorPayments.PayAppId
                    where payApp.SubcontractorId == currentSubcontractorId
                    select subcontractorPayments;
            }
            else
            {
                model = null;
            }

            return model;
        }

        [EnableQuery]
        public ActionResult<IQueryable<PayAppPaymentsVw>> Get()
        {
            if (User.Identity?.IsServiceUser() == true)
            {
                return BadRequest();
            }

            var model = GetSecureModel().OrderBy(p => p.PaymentId);
            if (model == null)
                return BadRequest();

            return Ok(model);
        }

        [EnableQuery]
        public ActionResult<PayAppPaymentsVw> Get([FromRoute] int key)
        {
            if (User.Identity?.IsServiceUser() == true)
            {
                return BadRequest();
            }

            var model = GetSecureModel().FirstOrDefault(pap => pap.PaymentId == key);
            if (model == null)
            {
                return NotFound();
            }
            return Ok(model);
        }

        public async Task<IActionResult> Post([FromBody] PayAppPaymentsVw model)
        {
            Console.WriteLine("THIS IS PAYAPP PAYMENT POST");
            if (User.Identity.IsServiceUser())
            {
                return BadRequest();
            }

            // if (!User.Identity.CanServiceAccountMakePayAppPaymentsRecord(model.PayAppId))
            // {
            //     return BadRequest("Insufficient permissions to create PayAppPayment for this PayApp");
            // }

            // if (!ModelState.IsValid)
            // {
            //     return BadRequest(ModelState);
            // }

            var databaseModel = new PayAppPayment();
            _mapper.Map(model, databaseModel);

            var user = User.Identity?.Name;
            Console.WriteLine($"user: {user}");

            databaseModel.JsonAttributes = _financialSecurityService.ProtectJsonAttributes(databaseModel.JsonAttributes);
            databaseModel.CreateDt = DateTimeOffset.UtcNow;
            databaseModel.LastUpdateDt = DateTimeOffset.UtcNow;
            databaseModel.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            databaseModel.CreatedByUser = User.Identity?.Name ?? "Unknown";

            Console.WriteLine($"databaseModel.LastUpdateUser: {databaseModel.LastUpdateUser}");

            // if (!TryValidateModel(databaseModel))
            // {
            //     return BadRequest(ModelState);
            // }

            _context.PayAppPayments.Add(databaseModel);

            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine("has been saved sucessfully");
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? "No inner exception";
                return BadRequest($"Database error: {ex.Message}. Inner: {innerMessage}");
            }

            model.PayAppId = databaseModel.PayAppId;
            //model.PaymentId = databaseModel.PaymentId;

            // Reconcile payment amounts
            try
            {
                var httpContextAccessor = HttpContext.RequestServices.GetService<IHttpContextAccessor>();
                var payAppPaymentService = new PayAppPaymentService(model.PayAppId, httpContextAccessor);

                await payAppPaymentService.ReconcileTotalDollarAmount();
                _logger.LogInformation("Payment reconciliation completed for PayApp {PayAppId}", model.PayAppId);
                Console.WriteLine($"Payment reconciliation completed for PayApp {model.PayAppId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during payment reconciliation for PayApp {PayAppId}", model.PayAppId);
                // Continue - don't fail the whole operation
            }

            var resultModel = _mapper.Map<PayAppPaymentsVw>(databaseModel);
            return Created(resultModel);
        }

        public async Task<IActionResult> Patch(int key, [FromBody] PayAppPaymentsVw patch)
        {
            if (patch == null)
                return BadRequest("No patch data provided");

            if (User.Identity?.IsServiceUser() == true)
            {
                return BadRequest();
            }

            var databaseModel = await _context.PayAppPayments.FirstOrDefaultAsync(pap => pap.PaymentId == key && pap.DeleteDt == null);
            var createdByUser = databaseModel.CreatedByUser;

            if (databaseModel == null)
                return NotFound();

            var originalPaymentId = databaseModel.PaymentId;
            var originalPayAppId = databaseModel.PayAppId;
            var originalCreateDt = databaseModel.CreateDt;
            _mapper.Map(patch, databaseModel);

            // var model = new PayAppPaymentsVw();
            // _mapper.Map(databaseModel, model);

            // patch.PayAppId = model.PayAppId; // Ensure PayAppId is preserved
            // _mapper.Map(model, databaseModel);

            databaseModel.JsonAttributes = _financialSecurityService.ProtectJsonAttributes(databaseModel.JsonAttributes);
            databaseModel.PaymentId = originalPaymentId;
            databaseModel.PayAppId = originalPayAppId;
            databaseModel.CreateDt = originalCreateDt;
            databaseModel.LastUpdateDt = DateTimeOffset.UtcNow;
            databaseModel.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            databaseModel.CreatedByUser = createdByUser;

            // if (!TryValidateModel(model))
            // {
            //     return BadRequest(ModelState);
            // }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating PayAppPayment {PaymentId}", key);
                var innerMessage = ex.InnerException?.Message ?? "No inner exception";
                Console.WriteLine($"Database error: {ex.Message}. Inner: {innerMessage}");
                return BadRequest($"Database error: {ex.Message}. Inner: {innerMessage}");
            }

            // Reconcile payment amounts
            try
            {
                var httpContextAccessor = HttpContext.RequestServices.GetService<IHttpContextAccessor>();
                var payAppPaymentService = new PayAppPaymentService(databaseModel.PayAppId, httpContextAccessor);

                await payAppPaymentService.ReconcileTotalDollarAmount();
                _logger.LogInformation("Payment reconciliation completed for PayApp {PayAppId} after update", databaseModel.PayAppId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during payment reconciliation for PayApp {PayAppId} after update", databaseModel.PayAppId);
                // Continue - don't fail the whole operation
            }

            return Ok(databaseModel);
        }

        public async Task<IActionResult> Delete(int key)
        {
            // if (User.Identity?.IsServiceUser() == true)
            // {
            //     return BadRequest();
            // }

            var model = await _context.PayAppPayments.FirstOrDefaultAsync(pap => pap.PaymentId == key && pap.DeleteDt == null);

            if (model == null)
                return NotFound();

            model.DeleteDt = DateTimeOffset.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error deleting PayAppPayment {PaymentId}", key);
                return BadRequest($"Database error: {e.Message}");
            }

            // Reconcile payment amounts after deletion
            try
            {
                var httpContextAccessor = HttpContext.RequestServices.GetService<IHttpContextAccessor>();
                var payAppPaymentService = new PayAppPaymentService(model.PayAppId, httpContextAccessor);

                await payAppPaymentService.ReconcileTotalDollarAmount();

                Console.WriteLine($"Reconciliation completed for PayApp ID {model.PayAppId}");
                _logger.LogInformation("Payment reconciliation completed for PayApp {PayAppId} after deletion", model.PayAppId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during payment reconciliation for PayApp {PayAppId} after deletion", model.PayAppId);
                // Continue - don't fail the whole operation
            }

            return NoContent();
        }


    }
}