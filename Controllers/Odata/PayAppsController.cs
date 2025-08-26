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
using NewVivaApi.Services;
using Microsoft.Extensions.Logging;
using NewVivaApi.Extensions;
using System.Text.Json;
using Microsoft.AspNet.Identity;

namespace NewVivaApi.Controllers.OData
{
    // [Authorize]
    public class PayAppsController : ODataController
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        // private readonly IFinancialSecurityService _financialSecurityService;
        // private readonly IPayAppPaymentService _payAppPaymentService;
        private readonly EmailService _emailService;
        // private readonly IWebHookService _webHookService;
        private readonly PayAppPaymentService _payAppPaymentService;
        private readonly ILogger<PayAppsController> _logger;

        public PayAppsController(AppDbContext context, ILogger<PayAppsController> logger, EmailService emailService, IMapper mapper /*, IFinancialSecurityService financialSecurityService */)
        {
            _context = context;
            _mapper = mapper;
            // _financialSecurityService = financialSecurityService;
            _logger = logger;
            _emailService = emailService;
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
        [HttpGet]
        public ActionResult Get()
        {
            var model = _context.PayAppsVws;

            if (!model.Any())
                return BadRequest("No records found.");

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
        public async Task<IActionResult> Post([FromBody] JsonElement data)
        {
            _logger.LogInformation("POST method called");
            Console.WriteLine("POST method called");
            Console.WriteLine($"data: {data}");
            try
            {
                var extractedData = ExtractPayAppData(data);
                Console.WriteLine($"Extracted PayApp data: VivaPayAppId={extractedData.VivaPayAppId}, SubcontractorProjectId={extractedData.SubcontractorProjectId}, StatusId={extractedData.StatusId}, RequestedAmount={extractedData.RequestedAmount}");

                // Manual validation
                var validationErrors = ValidatePayAppData(extractedData);
                if (validationErrors.Any())
                {
                    return BadRequest(new { errors = validationErrors });
                }

                // Convert to your PayAppsVw model
                var model = new PayAppsVw
                {
                    VivaPayAppId = extractedData.VivaPayAppId,
                    SubcontractorProjectId = extractedData.SubcontractorProjectId,
                    StatusId = extractedData.StatusId,
                    RequestedAmount = extractedData.RequestedAmount,
                    ApprovedAmount = extractedData.ApprovedAmount,
                    JsonAttributes = extractedData.JsonAttributes
                };

                if (model != null)
                {
                    _logger.LogInformation("Model received: PayAppId={PayAppId}, SubcontractorProjectId={SubcontractorProjectId}, StatusId={StatusId}, RequestedAmount={RequestedAmount}",
                        model.PayAppId, model.SubcontractorProjectId, model.StatusId, model.RequestedAmount);
                }

                //auth checks
                // if (User.Identity.IsServiceUser())
                // {
                //     return BadRequest();
                // }
                // if (!User.Identity.CanServiceAccountMakePayAppsRecord(model.SubcontractorProjectId))
                // {
                //     return BadRequest();
                // }

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

                Console.WriteLine("about to add database model to context");
                _context.PayApps.Add(databaseModel);

                try
                {
                    Console.WriteLine("about to save changes to context");
                    await _context.SaveChangesAsync();

                }
                catch (DbUpdateException ex)
                {
                    var innerMessage = ex.InnerException?.Message ?? "No inner exception";
                    return BadRequest($"Database error: {ex.Message}. Inner: {innerMessage}");
                }
                Console.WriteLine("PayApp saved successfully");
                model.PayAppId = databaseModel.PayAppId;

                var resultModel = _mapper.Map<PayAppsVw>(databaseModel);

                // Reconcile payment amounts
                try
                {
                    var httpContextAccessor = HttpContext.RequestServices.GetService<IHttpContextAccessor>();
                    var paymentService = new PayAppPaymentService(model.PayAppId, httpContextAccessor);

                    _logger.LogInformation("Starting payment reconciliation for PayApp {PayAppId}", model.PayAppId);
                    Console.WriteLine($"Starting payment reconciliation for PayApp ID {model.PayAppId}");

                    await paymentService.ReconcileTotalDollarAmount();

                    Console.WriteLine($"Reconciliation completed for PayApp ID {model.PayAppId}");
                    _logger.LogInformation("Payment reconciliation completed for PayApp {PayAppId}", model.PayAppId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during payment reconciliation for PayApp {PayAppId}", model.PayAppId);
                    // Continue - don't fail the whole operation
                }

                // Send notifications if status > 1
                if (model.StatusId > 1)
                {
                    PayAppsVw spv = _context.PayAppsVws.FirstOrDefault(l => l.PayAppId == databaseModel.PayAppId);
                    await _emailService.sendPayAppToApproveEmail(User.Identity.GetUserId(), spv.GeneralContractorId);
                    await _emailService.sendVivaNotificationNewPayApp(User.Identity.GetUserId(), spv.ProjectId, spv.GeneralContractorId, spv.PayAppId);
                }

                return Created($"PayApps({model.PayAppId})", model);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"PayApp creation error: {ex.Message}");
                return StatusCode(500, new { Type = "error", Message = "Internal server error" });
            }

        }

        // Helper methods
        private PayAppDataModel ExtractPayAppData(JsonElement data)
        {
            Console.WriteLine("Extracting PayApp data from JSON...");

            return new PayAppDataModel
            {
                VivaPayAppId = GetJsonProperty(data, "VivaPayAppID"),
                SubcontractorProjectId = GetJsonInt(data, "SubcontractorProjectID"),
                StatusId = GetJsonInt(data, "StatusID"),
                RequestedAmount = GetJsonDecimal(data, "RequestedAmount"),
                ApprovedAmount = GetJsonNullableDecimal(data, "ApprovedAmount"),
                JsonAttributes = GetJsonProperty(data, "JsonAttributes")
            };
        }

        private List<string> ValidatePayAppData(PayAppDataModel data)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(data.VivaPayAppId))
                errors.Add("VivaPayAppId is required");

            if (data.SubcontractorProjectId <= 0)
                errors.Add("Valid SubcontractorProjectId is required");

            if (data.StatusId <= 0)
                errors.Add("Valid StatusId is required");

            if (data.RequestedAmount <= 0)
                errors.Add("RequestedAmount must be greater than 0");

            return errors;
        }

        private string GetJsonProperty(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var prop) ? prop.GetString() ?? "" : "";
        }

        private int GetJsonInt(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number)
                    return prop.GetInt32();
                else if (prop.ValueKind == JsonValueKind.String)
                    int.TryParse(prop.GetString(), out int result);
            }
            return 0;
        }

        private decimal GetJsonDecimal(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number)
                    return prop.GetDecimal();
                else if (prop.ValueKind == JsonValueKind.String)
                    decimal.TryParse(prop.GetString(), out decimal result);
            }
            return 0;
        }

        private decimal? GetJsonNullableDecimal(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number)
                    return prop.GetDecimal();
                else if (prop.ValueKind == JsonValueKind.String)
                {
                    if (decimal.TryParse(prop.GetString(), out decimal result))
                        return result;
                }
            }
            return null;
        }

    }

    public class PayAppDataModel
    {
        public string VivaPayAppId { get; set; }
        public int SubcontractorProjectId { get; set; }
        public int StatusId { get; set; }
        public decimal RequestedAmount { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public string JsonAttributes { get; set; }
    }

}
