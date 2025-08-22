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
using AutoMapper;
using NewVivaApi.Authentication;
using NewVivaApi.Extensions;
using Newtonsoft.Json.Linq;

namespace NewVivaApi.Controllers.Odata
{
    //[Authorize]
    public class SubcontractorsController : ODataController
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // private readonly IFinancialSecurityService _financialSecurityService;
        // private readonly IEmailService _emailService;
        // private readonly IUserManager _userManager;

        public SubcontractorsController(
            AppDbContext context,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager
            )
        {
           _context = context;
            _mapper = mapper;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        /*
        private IQueryable<SubcontractorsVw> GetSecureModel()
        {
            var identity = User.Identity;

            if (identity.IsServiceUser())
                return null;

            if (identity.IsVivaUser())
            {
                return _context.Subcontractors_vw.OrderBy(x => x.SubcontractorName);
            }

            if (identity.IsGeneralContractor())
            {
                int contractorId = identity.GetGeneralContractorID();
                var subcontractorIds = _context.SubcontractorProjects_vw
                    .Where(x => x.GeneralContractorID == contractorId)
                    .Select(x => x.SubcontractorID).ToList();

                return _context.Subcontractors_vw
                    .Where(x => subcontractorIds.Contains(x.SubcontractorID ?? 0))
                    .OrderBy(x => x.SubcontractorName);
            }

            if (identity.IsSubContractor())
            {
                int subContractorID = identity.GetSubcontractorID();
                return _context.Subcontractors_vw.Where(x => x.SubcontractorID == subContractorID);
            }

            return null;
        }
        */

        [EnableQuery]
        public ActionResult Get()
        {
            var model = _context.SubcontractorsVws  
                .OrderBy(s => s.SubcontractorId)
                .Select(s => new
                {
                    SubcontractorID = s.SubcontractorId,
                    SubcontractorName = s.SubcontractorName,
                    VivaSubcontractorID = s.VivaSubcontractorId,
                    StatusID = s.StatusId,
                    CreatedByUser = s.CreatedByUser,
                    JsonAttributes = s.JsonAttributes
                });

            if (model == null)
                return BadRequest();

            return Ok(model);
        }

        [EnableQuery]
        public ActionResult<SubcontractorsVw> Get([FromRoute] int key)
        {
            var model = _context.SubcontractorsVws
                .Where(s => s.SubcontractorId == key)
                .Select(s => new
                {
                    SubcontractorID = s.SubcontractorId,
                    SubcontractorName = s.SubcontractorName,
                    VivaSubcontractorID = s.VivaSubcontractorId,
                    StatusID = s.StatusId,
                    CreatedByUser = s.CreatedByUser,
                    JsonAttributes = s.JsonAttributes
                })
                .FirstOrDefault(); 
            if (model == null)
                return NotFound();

            return Ok(model);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SubcontractorsVw model)
        {
            /*
            if (User.Identity.IsServiceUser())
                return BadRequest();
            */
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var dbModel = _mapper.Map<Subcontractor>(model);

            //dbModel.JsonAttributes = FinancialSecurityService.protectJsonAttributes(model.JsonAttributes);
            dbModel.CreateDt = DateTime.UtcNow;
            dbModel.LastUpdateDt = DateTime.UtcNow;
            dbModel.LastUpdateUser = "deki@steeleconsult.com";
            dbModel.CreatedByUser = "deki@steeleconsult.com";

            _context.Subcontractors.Add(dbModel);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException?.Message ?? "No inner exception";
                return BadRequest($"Database error: {ex.Message}. Inner: {innerMessage}");
            }

            model.SubcontractorId = dbModel.SubcontractorId;
            var resultModel = _mapper.Map<SubcontractorsVw>(dbModel);

            //var es = new EmailService();
            //es.sendAdminEmailNewSubcontractor(User.Identity.GetUserId(), model.SubcontractorID);

            await RegisterNewSubcontractorUser(resultModel);
            return Created(resultModel);
        }

        public async Task RegisterNewSubcontractorUser(SubcontractorsVw model)
        {
            Console.WriteLine($"Model: {model}");
            Console.WriteLine($"JsonAttributes: {model?.JsonAttributes}");
            if (User.Identity.IsServiceUser())
            {
                return;
            }

            // Check if JsonAttributes exists
            if (string.IsNullOrEmpty(model.JsonAttributes))
            {
                Console.WriteLine("JsonAttributes is null or empty");
                return;
            }

            JObject jsonAttributes = JObject.Parse(model.JsonAttributes);

            if (jsonAttributes["ContactEmail"] == null)
            {
                Console.WriteLine("ContactEmail not found in JsonAttributes");
                return;
            }

            string userName = jsonAttributes["ContactEmail"].ToString();

            ApplicationUser existingUser = await _userManager.FindByEmailAsync(userName);
            if (existingUser != null && existingUser.Id != null)
            {
                //User Already exists, don't create them.
                return;
            }

            if (existingUser != null && existingUser.Id != null)
            {
                //User Already exists, don't create them.
                return;
            }

            string contactName = jsonAttributes["Contact"].ToString();
            string[] names = contactName.Split(' ');

            string firstName = "First Name";
            string lastName = "Last Name";

            if (names.Length < 2)
            {
                firstName = contactName;
            }
            else
            {
                firstName = names[0];
                lastName = names[1];
            }

            // Generate password
            var requirements = new PasswordRequirements
            {
                RequireNumber = true,
                RequireSymbol = true,
                RequireLowercase = true,
                RequireUppercase = true,
                MinimumLength = 10,
                MaximumLength = 16
            };

            string generatedPassword = PasswordGenerationService.GeneratePassword(requirements);
            Console.WriteLine($"Generated password for user: {userName}");

            RegisterSystemUserModel newSubContractorUser = new RegisterSystemUserModel(_context, _userManager, _httpContextAccessor)
            {
                UserName = userName,
                FirstName = firstName,
                LastName = lastName,
                //PhoneNumber = phoneNumber,
                Password = generatedPassword,
                ConfirmPassword = generatedPassword,
                CompanyID = model.SubcontractorId,
                isAdminTF = false,
                isGCTF = false,
                isSCTF = true,
                gcApproveTF = true
            };

            // Register the user
            var creatorUserName = User?.Identity?.Name ?? string.Empty;
            Console.WriteLine($"Registering user: {userName} by {creatorUserName}");

            await newSubContractorUser.RegisterAsync(creatorUserName);
            Console.WriteLine("User registration completed.");

            return;

        }

        [HttpPatch]
        public async Task<IActionResult> Patch(int key, [FromBody] SubcontractorsVw patch)
        {
            // if (User.Identity.IsServiceUser())
            //     return BadRequest();

            var dbModel = await _context.Subcontractors.FindAsync(key);
            if (dbModel == null)
                return NotFound();

            _mapper.Map(patch, dbModel);
            dbModel.SubcontractorId = key; //manually clear the ID to avoid issues with OData patching

            //dbModel.JsonAttributes = FinancialSecurityService.protectJsonAttributes(model.JsonAttributes);
            dbModel.LastUpdateDt = DateTime.UtcNow;
            dbModel.LastUpdateUser = "deki@steeleconsult.com"; //temp

            // var validationErrors = dbModel.Validate();
            // if (validationErrors.Any())
            // {
            //     foreach (var error in validationErrors)
            //         ModelState.AddModelError(string.Empty, error);
            //     return BadRequest(ModelState);
            // }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException?.Message ?? "No inner exception";
                return BadRequest($"Database error: {ex.Message}. Inner: {innerMessage}");
            }

            var updatedViewModel = _mapper.Map<SubcontractorsVw>(dbModel);
            RegisterNewSubcontractorUser(updatedViewModel);
            return Updated(updatedViewModel);
        }

        public async Task<IActionResult> Delete([FromRoute] int key)
        {
            // if (User.Identity.IsServiceUser())
            //     return BadRequest();

            var model = await _context.Subcontractors.FindAsync(key);
            if (model == null)
                return NotFound();

            //_context.Subcontractors.Remove(model);
            model.DeleteDt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();

            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException?.Message ?? "No inner exception";
                return BadRequest($"Database error: {ex.Message}. Inner: {innerMessage}");
            }

            return NoContent();
        }


    }
}