using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;
using NewVivaApi.Models;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.OData;
using NewVivaApi.Data;
using NewVivaApi.Models;

namespace NewVivaApi.Controllers
{
    // Remove [Authorize] for now - we'll add it back once we implement auth
    // [Authorize]
    public class PayAppsController : ODataController
    {
        private readonly AppDbContext _context;
        // private readonly IMapper _mapper;
        // private readonly IFinancialSecurityService _financialSecurityService;
        // private readonly IPayAppPaymentService _payAppPaymentService;
        // private readonly IEmailService _emailService;
        // private readonly IWebHookService _webHookService;

        public PayAppsController(AppDbContext context)
        {
            _context = context;
            // _mapper = mapper;
            // _financialSecurityService = financialSecurityService;
            // etc.
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


        //POST does not work because of some complication with PayAppHistory
        public async Task<IActionResult> Post([FromBody] PayAppsVw model)
        {
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

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate that the SubcontractorProject exists
            var subcontractorProjectExists = await _context.SubcontractorProjects
                .AnyAsync(sp => sp.SubcontractorProjectId == model.SubcontractorProjectId);

            if (!subcontractorProjectExists)
            {
                return BadRequest($"SubcontractorProject with ID {model.SubcontractorProjectId} does not exist.");
            }

            var databaseModel = new PayApp
            {
                VivaPayAppId = model.VivaPayAppId,
                SubcontractorProjectId = model.SubcontractorProjectId,
                StatusId = model.StatusId,
                RequestedAmount = model.RequestedAmount,
                ApprovedAmount = model.ApprovedAmount,
                JsonAttributes = model.JsonAttributes, // TODO: Add financial security protection
                HistoryAttributes = "{\"Event\":\"Created\",\"LowestPermToView\":\"SubContractor\"}",
                ApprovalDt = null, // Set when approved
                CreateDt = DateTimeOffset.UtcNow,
                LastUpdateDt = DateTimeOffset.UtcNow,
                LastUpdateUser = User.Identity?.Name ?? "System",
                CreatedByUser = User.Identity?.Name ?? "System"


            };

            // TODO: Add financial security protection
            // databaseModel.JsonAttributes = _financialSecurityService.ProtectJsonAttributes(model.JsonAttributes);

            _context.PayApps.Add(databaseModel);

            try
            {
                await _context.SaveChangesAsync();
                var historyRecord = new PayAppHistory
                {
                    PayAppId = databaseModel.PayAppId,
                    CreateDt = DateTimeOffset.UtcNow,
                    LastUpdateUser = User.Identity?.Name ?? "System",
                    LastUpdateDt = DateTimeOffset.UtcNow,
                    Event = "Created",
                    LowestPermToView = "Subcontractor"
                };

                _context.PayAppHistories.Add(historyRecord);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Successfully created PayApp {databaseModel.PayAppId} with history record");

            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException?.Message ?? "No inner exception";
                return BadRequest($"Database error: {ex.Message}. Inner: {innerMessage}");
            }

            model.PayAppId = databaseModel.PayAppId;

            // TODO: Add PayAppPaymentService back
            // _payAppPaymentService.ReconcileTotalDollarAmount(model.PayAppId);

            // TODO: Add email service back
            /*
            if (model.StatusId > 1)
            {
                // Send approval emails
                // _emailService.SendPayAppToApproveEmail(...);
                // _emailService.SendVivaNotificationNewPayApp(...);
            }
            */

            return Created(model);
        }

    }
}
