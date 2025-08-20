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

namespace NewVivaApi.Controllers.OData
{
    //[Authorize]
    public class PayAppPaymentsController : ODataController
    {
        private readonly AppDbContext _context;
        //private readonly ODataValidationSettings _validationSettings = new ODataValidationSettings();
        private readonly IMapper _mapper;
        private readonly ILogger<PayAppPaymentsController> _logger;
        //private readonly FinancialSecurityService _financialSecurityService;

        public PayAppPaymentsController(
            AppDbContext context, 
            IMapper mapper, 
            ILogger<PayAppPaymentsController> logger)
            //FinancialSecurityService financialSecurityService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            //_financialSecurityService = financialSecurityService;
        }

        private IQueryable<PayAppPaymentsVw> GetSecureModel()
        {
            /*
            if (User.Identity?.IsServiceUser() == true)
            {
                return Enumerable.Empty<PayAppPaymentsVw>().AsQueryable();
            }

            if (User.Identity?.IsVivaUser() == true)
            {
                return _context.PayAppPaymentsVw;
            }
            else if (User.Identity?.IsGeneralContractor() == true)
            {
                var currentGeneralContractorId = User.Identity.GetGeneralContractorID();
                return from payApp in _context.PayAppsVw
                       join generalContractorPayments in _context.PayAppPaymentsVw on payApp.PayAppId equals generalContractorPayments.PayAppId
                       where payApp.GeneralContractorId == currentGeneralContractorId
                       select generalContractorPayments;
            }
            else if (User.Identity?.IsSubContractor() == true)
            {
                var currentSubcontractorId = User.Identity.GetSubcontractorID();
                return from payApp in _context.PayAppsVw
                       join subcontractorPayments in _context.PayAppPaymentsVw on payApp.PayAppId equals subcontractorPayments.PayAppId
                       where payApp.SubcontractorId == currentSubcontractorId
                       select subcontractorPayments;
            }
            */

            return Enumerable.Empty<PayAppPaymentsVw>().AsQueryable();
        }

        [EnableQuery]
        public ActionResult<IQueryable<PayAppPaymentsVw>> Get()
        {
            // if (User.Identity?.IsServiceUser() == true)
            // {
            //     return BadRequest();
            // }

            var model = _context.PayAppPaymentsVws.OrderBy(p => p.PaymentId);
            if (model == null)
                return BadRequest();

            return Ok(model);
        }

        [EnableQuery]
        public ActionResult<PayAppPaymentsVw> Get([FromRoute] int key)
        {
            // if (User.Identity?.IsServiceUser() == true)
            // {
            //     return BadRequest();
            // }

            // var model = GetSecureModel().FirstOrDefault(pap => pap.PaymentId == key);
            // if (model == null)
            // {
            //     return NotFound();
            // }

            var model = _context.PayAppPaymentsVws.FirstOrDefault(p => p.PaymentId == key);
            if (model == null)
                return NotFound();

            return Ok(model);
        }

        public async Task<IActionResult> Post([FromBody] PayAppPaymentsVw model)
        {
            // if (User.Identity?.IsServiceUser() == true)
            // {
            //     return BadRequest("Service users cannot create PayAppPayments");
            // }

            // if (!User.Identity.CanServiceAccountMakePayAppPaymentsRecord(model.PayAppId))
            // {
            //     return BadRequest("Insufficient permissions to create PayAppPayment for this PayApp");
            // }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var databaseModel = new PayAppPayment();
            _mapper.Map(model, databaseModel);

            //databaseModel.JsonAttributes = _financialSecurityService.ProtectJsonAttributes(databaseModel.JsonAttributes);
            databaseModel.CreateDt = DateTimeOffset.UtcNow;
            databaseModel.LastUpdateDt = DateTimeOffset.UtcNow;
            databaseModel.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            databaseModel.CreatedByUser = User.Identity?.Name ?? "Unknown";

            // if (!TryValidateModel(databaseModel))
            // {
            //     return BadRequest(ModelState);
            // }

            _context.PayAppPayments.Add(databaseModel);
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? "No inner exception";
                return BadRequest($"Database error: {ex.Message}. Inner: {innerMessage}");
            }

            model.PayAppId = databaseModel.PayAppId;

            // Reconcile payment amounts
            try
            {
                var httpContextAccessor = HttpContext.RequestServices.GetService<IHttpContextAccessor>();
                var payAppPaymentService = new PayAppPaymentService(model.PayAppId, httpContextAccessor);
                
                await payAppPaymentService.ReconcileTotalDollarAmount();
                _logger.LogInformation("Payment reconciliation completed for PayApp {PayAppId}", model.PayAppId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during payment reconciliation for PayApp {PayAppId}", model.PayAppId);
                // Continue - don't fail the whole operation
            }

            var resultModel = _mapper.Map<PayAppPaymentsVw>(model);
            return Created($"PayAppPayments({model.PayAppId})", resultModel);
        }

        public async Task<IActionResult> Patch(int key, [FromBody] PayAppPaymentsVw patch)
        {
            if (patch == null)
                return BadRequest("No patch data provided");

            // if (User.Identity?.IsServiceUser() == true)
            // {
            //     return BadRequest();
            // }

            var databaseModel = await _context.PayAppPayments.FirstOrDefaultAsync(pap => pap.PaymentId == key && pap.DeleteDt == null);
            
            if (databaseModel == null)
                return NotFound();

            var originalPaymentId = databaseModel.PaymentId;
            var originalPayAppId = databaseModel.PayAppId;
            var originalCreateDt = databaseModel.CreateDt;
            var createdByUser = databaseModel.CreatedByUser;

            _mapper.Map(patch, databaseModel);

            // var model = new PayAppPaymentsVw();
            // _mapper.Map(databaseModel, model);

            // patch.PayAppId = model.PayAppId; // Ensure PayAppId is preserved
            // _mapper.Map(model, databaseModel);

            //databaseModel.JsonAttributes = _financialSecurityService.ProtectJsonAttributes(model.JsonAttributes);
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