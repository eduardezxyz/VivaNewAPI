using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.OData;
using AutoMapper;
using NewVivaApi.Data;
using NewVivaApi.Models;
using Microsoft.Extensions.Logging;

namespace NewVivaApi.Controllers.OData
{
    // [Authorize]
    public class PayAppsController : ODataController
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        // private readonly IFinancialSecurityService _financialSecurityService;
        // private readonly IPayAppPaymentService _payAppPaymentService;
        // private readonly IEmailService _emailService;
        // private readonly IWebHookService _webHookService;
        private readonly ILogger<PayAppsController> _logger;

        public PayAppsController(AppDbContext context, ILogger<PayAppsController> logger, IMapper mapper /*, IFinancialSecurityService financialSecurityService */)
        {
            _context = context;
            _mapper = mapper;
            // _financialSecurityService = financialSecurityService;
            _logger = logger;
        }

        private IQueryable<PayAppsVw> GetSecureModel()
        {
            // Temporarily return all PayApps - we'll add auth logic later
            /*
            IQueryable<PayAppsVw> model;

            if (User.Identity.IsServiceUser())
            {
                return null;
            }

            if (User.Identity.IsVivaUser())
            {
                model = _context.PayAppsVws;                              
            }
            else if (User.Identity.IsGeneralContractor())
            {
                int? currentGeneralContractorId = User.Identity.GetGeneralContractorID();
                model = _context.PayAppsVws.Where(payApp =>
                    payApp.GeneralContractorId == currentGeneralContractorId);
            }
            else if (User.Identity.IsSubContractor())
            {
                int? currentSubcontractorId = User.Identity.GetSubcontractorID();
                model = _context.PayAppsVws.Where(payApp =>
                    payApp.SubcontractorId == currentSubcontractorId);
            }
            else
            {
                model = null;
            }

            return model;
            */

            // For now, return all PayApps
            return _context.PayAppsVws;
        }


        [EnableQuery]
        public ActionResult<PayAppsVw> Get()
        {
            //auth check
            /*
            if (User.Identity.IsServiceUser())
            {
                return null;
            }
            */

            var model = _context.PayAppsVws.OrderBy(p => p.PayAppId);
            if (model == null)
                return BadRequest();

            return Ok(model);
        }


        [EnableQuery]
        public ActionResult<PayAppsVw> Get([FromRoute] int key)
        {
            //auth check
            /*
            if (User.Identity.IsServiceUser())
            {
                return null;
            }
            */

            var model = _context.PayAppsVws.FirstOrDefault(p => p.PayAppId == key);
            if (model == null)
                return NotFound();

            return Ok(model);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] PayAppsVw model)
        {   

            _logger.LogInformation("POST method called");
            
            if (model != null)
            {
                _logger.LogInformation("Model received: PayAppId={PayAppId}, SubcontractorProjectId={SubcontractorProjectId}, StatusId={StatusId}, RequestedAmount={RequestedAmount}", 
                    model.PayAppId, model.SubcontractorProjectId, model.StatusId, model.RequestedAmount);
            }
            
            _logger.LogInformation("ModelState.IsValid: {IsValid}", ModelState.IsValid);
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState errors:");
                foreach (var error in ModelState)
                {
                    _logger.LogWarning("Key: {Key}, Errors: {Errors}", error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
                }
            }

            //auth checks
            /*
            if (User.Identity.IsServiceUser())
            {
                return BadRequest();
            }

            if (!User.Identity.CanServiceAccountMakePayAppsRecord(model.SubcontractorProjectId))
            {
                return BadRequest();
            }
            */

            if (model == null)
            {
                return BadRequest("PayApp model is required");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verify the SubcontractorProject exists
            var subcontractorProjectExists = await _context.SubcontractorProjects
                .AnyAsync(sp => sp.SubcontractorProjectId == model.SubcontractorProjectId);
            
            if (!subcontractorProjectExists)
            {
                return BadRequest($"SubcontractorProject with ID {model.SubcontractorProjectId} does not exist");
            }

            var databaseModel = new PayApp();
            _mapper.Map(model, databaseModel);
            Console.WriteLine($"Creating PayApp with SubcontractorProjectId: {databaseModel.SubcontractorProjectId}");

            //databaseModel.JsonAttributes = _financialSecurityService.ProtectJsonAttributes(model.JsonAttributes);
            databaseModel.CreateDt = DateTimeOffset.UtcNow;
            databaseModel.LastUpdateDt = DateTimeOffset.UtcNow;
            databaseModel.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            databaseModel.CreatedByUser = User.Identity?.Name ?? "Unknown";

            Console.WriteLine("about to validate database model");
            // Validate the database model
            // if (!TryValidateModel(databaseModel))
            // {
            //     return BadRequest(ModelState);
            // }
            Console.WriteLine("about to add database model to context");
            _context.PayApps.Add(databaseModel);

            try
            {
                Console.WriteLine("about to save changes to context");
                await _context.SaveChangesAsync();

                // After saving, create the initial PayAppHistory record
                // var payAppHistory = new PayAppHistory
                // {
                //     PayAppId = databaseModel.PayAppId,
                //     Event = "Created",
                //     CreateDt = DateTimeOffset.UtcNow,
                //     LastUpdateDt = DateTimeOffset.UtcNow,
                //     LastUpdateUser = User.Identity?.Name ?? "Unknown",
                //     LowestPermToView = "Subcontractor" // or whatever the appropriate permission level should be
                // };
                
                //_context.PayAppHistories.Add(payAppHistory);
                //await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException?.Message ?? "No inner exception";
                return BadRequest($"Database error: {ex.Message}. Inner: {innerMessage}");
            }
            Console.WriteLine("PayApp saved successfully");
            model.PayAppId = databaseModel.PayAppId;

            var resultModel = _mapper.Map<PayAppsVw>(databaseModel);

            // // Reconcile payment amounts
            // var payAppPaymentService = new PayAppPaymentService(model.PayAppId);
            // await payAppPaymentService.ReconcileTotalDollarAmount();

            // // Send notifications if status > 1
            // if (model.StatusId > 1)
            // {
            //     var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //     var spv = await _context.PayAppsVw.FirstOrDefaultAsync(l => l.PayAppId == databaseModel.PayAppId);

            //     if (spv != null && userId != null)
            //     {
            //         await _emailService.SendPayAppToApproveEmail(userId, spv.GeneralContractorId);
            //         await _emailService.SendVivaNotificationNewPayApp(userId, spv.ProjectId, spv.GeneralContractorId, spv.PayAppId);
            //     }
            // }

            return Created($"PayApps({model.PayAppId})", model);
        }


    }
}
