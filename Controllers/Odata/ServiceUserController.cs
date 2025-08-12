using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Attributes;
using Microsoft.EntityFrameworkCore;
using NewVivaApi.Data;
using NewVivaApi.Models;
using NewVivaApi.Services;
using AutoMapper;
using System.Security.Claims;
using Newtonsoft.Json.Linq;
using System.Web;
using System;

namespace NewVivaApi.Controllers.OData
{
    //[Authorize]
    [Route("odata")]
    public class ServiceUserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        //private readonly FinancialSecurityService _financialSecurityService;
        //private readonly EmailService _emailService;
        //private readonly SubdomainService _subdomainService;
        private readonly ILogger<ServiceUserController> _logger;

        public ServiceUserController(
            AppDbContext context,
            IMapper mapper,
            //FinancialSecurityService financialSecurityService,
            //EmailService emailService,
            //SubdomainService subdomainService,
            ILogger<ServiceUserController> logger)
        {
            _context = context;
            _mapper = mapper;
            //_financialSecurityService = financialSecurityService;
            //_emailService = emailService;
            //_subdomainService = subdomainService;
            _logger = logger;
        }

        [HttpPost("CreateSubContractor")]
        public async Task<IActionResult> CreateSubContractor([FromBody] SubcontractorsVw model)
        {
            // if (!ModelState.IsValid)
            // {
            //     return BadRequest(ModelState);
            // }

            var databaseModel = new Subcontractor();
            _mapper.Map(model, databaseModel);

            // Encrypt JsonAttributes
            //string protectedJsonAttributes = _financialSecurityService.ProtectJsonAttributes(model.JsonAttributes);
            //databaseModel.JsonAttributes = protectedJsonAttributes;
            databaseModel.CreateDt = DateTimeOffset.UtcNow;
            databaseModel.LastUpdateDt = DateTimeOffset.UtcNow;
            databaseModel.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            databaseModel.CreatedByUser = User.Identity?.Name ?? "Unknown";

            // if (!TryValidateModel(databaseModel))
            // {
            //     return BadRequest(ModelState);
            // }

            _context.Subcontractors.Add(databaseModel);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error creating Subcontractor");
                return BadRequest($"Database error: {e.Message}. Inner: {e.InnerException?.Message}");
            }

            model.SubcontractorId = databaseModel.SubcontractorId;

            return Created($"Subcontractors({model.SubcontractorId})", model);
        }

        [HttpPost("CreateGeneralContractor")]
        public async Task<IActionResult> CreateGeneralContractor([FromBody] GeneralContractorsVw model)
        {
            // if (!ModelState.IsValid)
            // {
            //     return BadRequest(ModelState);
            // }

            // Encryption
            //string protectedJsonAttributes = _financialSecurityService.ProtectJsonAttributes(model.JsonAttributes);

            var databaseModel = new GeneralContractor();
            _mapper.Map(model, databaseModel);

            //databaseModel.JsonAttributes = protectedJsonAttributes;
            databaseModel.CreateDt = DateTimeOffset.UtcNow;
            databaseModel.LastUpdateDt = DateTimeOffset.UtcNow;
            databaseModel.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            databaseModel.LogoImage = model.LogoImage;
            databaseModel.DommainName = model.DommainName;
            databaseModel.CreatedByUser = User.Identity?.Name ?? "Unknown";

            // if (!TryValidateModel(databaseModel))
            // {
            //     return BadRequest(ModelState);
            // }

            _context.GeneralContractors.Add(databaseModel);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error creating GeneralContractor");
                return BadRequest($"Database error: {e.Message}. Inner: {e.InnerException?.Message}");
            }

            model.GeneralContractorId = databaseModel.GeneralContractorId;

            // TODO: Handle subdomain creation if needed
            // if (!string.IsNullOrWhiteSpace(model.DommainName))
            // {
            //     try
            //     {
            //         var name = await _subdomainService.AddNewSubdomain(model.DommainName);
            //         _logger.LogInformation("Created subdomain {DomainName} for GeneralContractor {GeneralContractorId}", 
            //             model.DommainName, model.GeneralContractorId);
            //     }
            //     catch (Exception ex)
            //     {
            //         _logger.LogError(ex, "Error creating subdomain {DomainName}", model.DommainName);
            //         // Continue - don't fail the whole operation for subdomain errors
            //     }
            // }

            return Created($"GeneralContractors({model.GeneralContractorId})", model);
        }

        [HttpPost("CreateProject")]
        public async Task<IActionResult> CreateProject([FromBody] ProjectsVw model)
        {
            //Console.WriteLine("Creating Project with model: " + model);
            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState is invalid");
                return BadRequest(ModelState);
            }

            var genContractorId = model.GeneralContractorId;

            //Console.WriteLine("ModelState is valid");
            var databaseModel = new Project();
            _mapper.Map(model, databaseModel);

            databaseModel.GeneralContractorId = genContractorId;

            //Console.WriteLine("Mapping completed, setting additional properties");
            databaseModel.CreateDt = DateTime.UtcNow;
            databaseModel.LastUpdateDt = DateTime.UtcNow;
            databaseModel.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            databaseModel.CreatedByUser = User.Identity?.Name ?? "Unknown";

            //Console.WriteLine("Creating Project with model: " + databaseModel);

            //Console.WriteLine("Setting additional properties completed");
            //ModelState.Remove("GeneralContractor");

            // Before validation, load the GeneralContractor

            var generalContractor = await _context.GeneralContractors
                .FirstOrDefaultAsync(gc => gc.GeneralContractorId == databaseModel.GeneralContractorId);

            if (generalContractor == null)
            {
                return BadRequest("GeneralContractorId doesn't exist");
            }

            // Assign the navigation property
            databaseModel.GeneralContractor = generalContractor;

            if (!TryValidateModel(databaseModel))
            {
                Console.WriteLine("Model validation failed");
                Console.WriteLine("ModelState errors: " + string.Join(", ", ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
                return BadRequest(ModelState);
            }

            var generalContractorExists = await _context.GeneralContractors
                .AnyAsync(p => p.GeneralContractorId == databaseModel.GeneralContractorId && 
                              p.CreatedByUser == User.Identity.Name);

            Console.WriteLine($"GeneralContractorId {databaseModel.GeneralContractorId} exists: {generalContractorExists}");
            if (!generalContractorExists)
            {
                Console.WriteLine("GeneralContractorId doesn't exist or you don't have permission to access it");
                return BadRequest("GeneralContractorId doesn't exist or you don't have permission to access it");
            }

            _context.Projects.Add(databaseModel);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error creating Project");
                Console.WriteLine($"Database error: {e.Message}. Inner: {e.InnerException?.Message}");
                return BadRequest($"Database error: {e.Message}. Inner: {e.InnerException?.Message}");
            }

            model.ProjectId = databaseModel.ProjectId;

            return Created($"Projects({model.ProjectId})", model);
        }

        [HttpPost("CreateSubcontractorProject")]
        public async Task<IActionResult> CreateSubcontractorProject([FromBody] SubcontractorProjectsVw model)
        {
            // if (!ModelState.IsValid)
            // {
            //     return BadRequest(ModelState);
            // }
            var subcontractorId = model.SubcontractorId;
            var projectId = model.ProjectId;

            var databaseModel = new SubcontractorProject();
            _mapper.Map(model, databaseModel);

            databaseModel.SubcontractorId = subcontractorId;
            databaseModel.ProjectId = projectId;
            databaseModel.CreateDt = DateTimeOffset.UtcNow;
            databaseModel.LastUpdateDt = DateTimeOffset.UtcNow;
            databaseModel.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            databaseModel.CreatedByUser = User.Identity?.Name ?? "Unknown";

            Console.WriteLine("Creating SubcontractorProject with model: " + databaseModel);

            var subcontractor = await _context.Subcontractors
                .FirstOrDefaultAsync(gc => gc.SubcontractorId == databaseModel.SubcontractorId);

            var project = await _context.Projects
                .FirstOrDefaultAsync(gc => gc.ProjectId == databaseModel.ProjectId);

            if (subcontractor == null){
                return BadRequest("SubcontractorId doesn't exist");
            }
            if (project == null){
                return BadRequest("ProjectId doesn't exist");
            }

            databaseModel.Subcontractor = subcontractor;
            databaseModel.Project = project;

            ModelState.Remove("ProjectName");
            ModelState.Remove("SubcontractorName");
            ModelState.Remove("GeneralContractorId");
            ModelState.Remove("Contact");
            ModelState.Remove("ContactEmail");

            // var subcontractorId = model.SubcontractorId;
            // var projectId = model.ProjectId;

            // if (!TryValidateModel(databaseModel))
            // {
            //     return BadRequest(ModelState);
            // }

            var projectExists = await _context.Projects
                .AnyAsync(p => p.ProjectId == databaseModel.ProjectId && 
                              p.CreatedByUser == User.Identity.Name);

            var subcontractorExists = await _context.Subcontractors
                .AnyAsync(p => p.SubcontractorId == databaseModel.SubcontractorId && 
                              p.CreatedByUser == User.Identity.Name);

            if (!projectExists)
            {
                return BadRequest("ProjectId doesn't exist or you don't have permission to access it");
            }

            if (!subcontractorExists)
            {
                return BadRequest("SubcontractorId doesn't exist or you don't have permission to access it");
            }

            _context.SubcontractorProjects.Add(databaseModel);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error creating SubcontractorProject");
                return BadRequest($"Database error: {e.Message}. Inner: {e.InnerException?.Message}");
            }

            model.SubcontractorProjectId = databaseModel.SubcontractorProjectId;

            return Created($"SubcontractorProjects({model.SubcontractorProjectId})", model);
        }

        [HttpPost("CreatePayApp")]
        public async Task<IActionResult> CreatePayApp([FromBody] PayAppsVw model) //NOT working 

        /* Database error: An error occurred while saving the entity changes. 
        See the inner exception for details.. 
        Inner: Cannot insert the value NULL into column 'Event', table 'VivaPayApp_DEV.dbo.PayAppHistory'; column does not allow nulls. INSERT fails.
        The statement has been terminated. */
        {
            // if (!ModelState.IsValid)
            // {
            //     return BadRequest(ModelState);
            // }

            if (model == null)
            {
                _logger.LogError("Model is null - JSON binding failed");
                return BadRequest("Model binding failed - check your JSON input");
            }

            var subcontractorProjectId = model.SubcontractorProjectId;
            var statusId = model.StatusId;

            var databaseModel = new PayApp();
            _mapper.Map(model, databaseModel);

            //databaseModel.JsonAttributes = _financialSecurityService.ProtectJsonAttributes(model.JsonAttributes);
            databaseModel.CreateDt = DateTimeOffset.UtcNow;
            databaseModel.LastUpdateDt = DateTimeOffset.UtcNow;
            databaseModel.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            databaseModel.CreatedByUser = User.Identity?.Name ?? "Unknown";
            //databaseModel.subcontractorProjectId = subcontractorProjectId;
            databaseModel.StatusId = statusId;

            if (databaseModel.StatusId != 2)
            {
                return BadRequest("Status ID has to be Submit (2)");
            }

            if (databaseModel.ApprovedAmount != null)
            {
                return BadRequest("Approved Amount must be null when creating a Pay App");
            }

            // Validate(databaseModel);
            // if (!TryValidateModel(databaseModel))
            // {
            //     return BadRequest(ModelState);
            // }

            // var subcontractorProjectExists = await _context.SubcontractorProjects
            //     .AnyAsync(p => p.SubcontractorProjectId == databaseModel.SubcontractorProjectId && 
            //                   p.CreatedByUser == User.Identity.Name);

            var subcontractorProjectExists = await _context.SubcontractorProjects
                .AnyAsync(p => p.SubcontractorProjectId == databaseModel.SubcontractorProjectId);

            var subcontractorProjectExists1 = await _context.SubcontractorProjects
                .AnyAsync(p => p.CreatedByUser == User.Identity.Name);

            if (!subcontractorProjectExists)
            {
                return BadRequest("SubcontractorProjectId doesn't exist or you don't have permission to access it");
            }

            if (!subcontractorProjectExists1)
            {
                return BadRequest("User doesn't exist or you don't have permission to access it");
            }

            _context.PayApps.Add(databaseModel);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error creating PayApp");
                return BadRequest($"Database error: {e.Message}. Inner: {e.InnerException?.Message}");
            }

            model.PayAppId = databaseModel.PayAppId;

            // Reconcile payment amounts
            try
            {
                var httpContextAccessor = HttpContext.RequestServices.GetService<IHttpContextAccessor>();
                var payAppPaymentService = new PayAppPaymentService(
                    model.PayAppId,httpContextAccessor);
                
                await payAppPaymentService.ReconcileTotalDollarAmount();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during payment reconciliation for PayApp {PayAppId}", model.PayAppId);
                // Continue - don't fail the whole operation
            }

            // // Send notifications
            // try
            // {
            //     var userId = "00915afc-f6a7-453f-92e3-04a7a15477b6"; // Hardcoded for debugging
            //     //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;   COMMENTED OUT FOR DEBUGGING
            //     var spv = await _context.PayAppsVw.FirstOrDefaultAsync(l => l.PayAppId == databaseModel.PayAppId);
                
            //     if (spv != null && userId != null)
            //     {
            //         await _emailService.SendVivaNotificationNewPayApp(userId, spv.ProjectId, spv.GeneralContractorId, spv.PayAppId);
            //     }
            // }
            // catch (Exception ex)
            // {
            //     _logger.LogError(ex, "Error sending email notification for PayApp {PayAppId}", model.PayAppId);
            //     // Continue - don't fail the whole operation
            // }

            return Created($"PayApps({model.PayAppId})", model);
        }

        [HttpPost("CreatePayAppPayment")]
        public async Task<IActionResult> CreatePayAppPayment([FromBody] PayAppPaymentsVw model)
        {
            // if (!ModelState.IsValid)
            // {
            //     return BadRequest(ModelState);
            // }

            var payAppId = model.PayAppId;

            var databaseModel = new PayAppPayment();
            _mapper.Map(model, databaseModel);

            //databaseModel.JsonAttributes = _financialSecurityService.ProtectJsonAttributes(databaseModel.JsonAttributes);
            databaseModel.CreateDt = DateTimeOffset.UtcNow;
            databaseModel.LastUpdateDt = DateTimeOffset.UtcNow;
            databaseModel.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            databaseModel.CreatedByUser = User.Identity?.Name ?? "Unknown";
            databaseModel.PayAppId = payAppId;

            var payApp = await _context.PayApps
                .FirstOrDefaultAsync(gc => gc.PayAppId == databaseModel.PayAppId);

            if (payApp == null)
            {
                return BadRequest("GeneralContractorId doesn't exist");
            }

            // Assign the navigation property
            databaseModel.PayApp = payApp;

            // if (!TryValidateModel(databaseModel))
            // {
            //     return BadRequest(ModelState);
            // }

            var payAppExists = await _context.PayApps
                .AnyAsync(p => p.PayAppId == databaseModel.PayAppId && 
                              p.CreatedByUser == User.Identity.Name);

            if (!payAppExists)
            {
                return BadRequest("PayAppId doesn't exist or you don't have permission to access it");
            }

            _context.PayAppPayments.Add(databaseModel);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error creating PayAppPayment");
                return BadRequest($"Database error: {e.Message}. Inner: {e.InnerException?.Message}");
            }

            model.PaymentId = databaseModel.PaymentId;

            // Reconcile payment amounts
            try
            {
                var httpContextAccessor = HttpContext.RequestServices.GetService<IHttpContextAccessor>();
                var payAppPaymentService = new PayAppPaymentService(model.PayAppId, httpContextAccessor);
                
                await payAppPaymentService.ReconcileTotalDollarAmount();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during payment reconciliation for PayApp {PayAppId}", model.PayAppId);
                // Continue - don't fail the whole operation
            }

            return Created($"PayAppPayments({model.PaymentId})", model);
        }

        [HttpPost("UpdatePayApp")]
        public async Task<IActionResult> UpdatePayApp([FromBody] PayAppsVw model) //same as CREATE 
        {
            if (model == null || model.PayAppId == 0)
            {
                return BadRequest("Invalid PayApp data: PayAppId is required.");
            }

            if (model.StatusId != 2 && model.StatusId != 3)
            {
                return BadRequest("Invalid Status ID");
            }

            if (model.StatusId == 3 && model.ApprovedAmount == null)
            {
                return BadRequest("Approved Amount cannot be null when the Status is Approved");
            }

            if (model.StatusId == 2 && model.ApprovedAmount != null)
            {
                return BadRequest("Approved Amount has to be null when the Status is Submit");
            }

            var databaseModel = await _context.PayApps
                .FirstOrDefaultAsync(p => p.PayAppId == model.PayAppId && 
                                         p.DeleteDt == null && 
                                         p.CreatedByUser == User.Identity.Name);

            if (databaseModel == null)
                return NotFound();

            int previousStatusId = databaseModel.StatusId;
            var createdByUser = databaseModel.CreatedByUser;

            // Map incoming values to DB entity
            _mapper.Map(model, databaseModel);

            JObject jsonAttributes = JObject.Parse(model.JsonAttributes ?? "{}");
            var paymentAccountNumber = jsonAttributes["PaymentAccountNumber"];
            string accountNum = paymentAccountNumber != null ? paymentAccountNumber.ToString() : "";

            //databaseModel.JsonAttributes = _financialSecurityService.ProtectJsonAttributes(model.JsonAttributes);
            databaseModel.LastUpdateDt = DateTimeOffset.UtcNow;
            databaseModel.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            databaseModel.CreatedByUser = createdByUser;

            if (!TryValidateModel(model))
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error updating PayApp {PayAppId}", model.PayAppId);
                return BadRequest($"Database error: {e.Message}");
            }

            // Reconcile payment amounts
            try
            {
                var httpContextAccessor = HttpContext.RequestServices.GetService<IHttpContextAccessor>();
                var payAppPaymentService = new PayAppPaymentService(model.PayAppId, httpContextAccessor);
                
                await payAppPaymentService.ReconcileTotalDollarAmount();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during payment reconciliation for PayApp {PayAppId}", model.PayAppId);
                // Continue - don't fail the whole operation
            }

            // Send notifications if status changed to approved
            // if (model.StatusId == 3 && previousStatusId != 3)
            // {
            //     try
            //     {
            //         var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //         var spv = await _context.PayAppsVw.FirstOrDefaultAsync(l => l.PayAppId == databaseModel.PayAppId);
                    
            //         if (spv != null && userId != null)
            //         {
            //             await _emailService.SendAdminPayAppApproved(userId, spv.ProjectId, spv.SubcontractorId, spv.PayAppId);
            //         }
            //     }
            //     catch (Exception ex)
            //     {
            //         _logger.LogError(ex, "Error sending approval email for PayApp {PayAppId}", model.PayAppId);
            //         // Continue - don't fail the whole operation
            //     }
            // }

            return Ok("PayApp updated successfully.");
        }

        // [HttpGet("GetPayApps")]
        // public IActionResult GetPayApps() //Needs USER Services
        // {
        //     var data = _context.PayAppsVw.Where(p => p.CreatedByUser == User.Identity.Name);
        //     return Ok(data);
        // }

        // [HttpGet("GetPayAppById")]
        // public IActionResult GetPayAppById([FromQuery] int PayAppId) //Needs USER Services
        // {
        //     var payApp = _context.PayAppsVw
        //         .FirstOrDefault(p => p.PayAppId == PayAppId && p.CreatedByUser == User.Identity.Name);
            
        //     if (payApp == null)
        //     {
        //         return NotFound();
        //     }
            
        //     return Ok(payApp);
        // }

        // [HttpGet("GetPayAppsHistory")]
        // public IActionResult GetPayAppsHistory() //Needs USER Services
        // {
        //     var payAppIds = _context.PayAppsVw
        //         .Where(p => p.CreatedByUser == User.Identity.Name)
        //         .Select(p => p.PayAppId);

        //     var payAppHistories = _context.PayAppHistoryVw
        //         .Where(h => payAppIds.Contains(h.PayAppId));

        //     return Ok(payAppHistories);
        // }

        // [HttpGet("GetPayAppsHistoryByPayAppId")]
        // public IActionResult GetPayAppsHistoryByPayAppId([FromQuery] int PayAppId) //Needs USER Services
        // {
        //     bool isCreatorOfPayApp = _context.PayAppsVw
        //         .Any(p => p.CreatedByUser == User.Identity.Name && p.PayAppId == PayAppId);

        //     if (!isCreatorOfPayApp)
        //     {
        //         return NotFound();
        //     }

        //     var payAppHistories = _context.PayAppHistoryVw
        //         .Where(h => h.PayAppId == PayAppId);

        //     return Ok(payAppHistories);
        // }
    }
}