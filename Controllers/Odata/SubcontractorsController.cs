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

namespace NewVivaApi.Controllers.Odata
{
    //[Authorize]
    public class SubcontractorsController : ODataController
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        // private readonly IFinancialSecurityService _financialSecurityService;
        // private readonly IEmailService _emailService;
        // private readonly IUserManager _userManager;

        public SubcontractorsController(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
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
            try
            {
                // // Check if the user is a service user and return early if true
                // if (model.IsServiceUser())
                // {
                //     return;
                // }

                // // Parse the JSON attributes and extract user information

                // var jsonAttributes = JsonSerializer.Deserialize<JsonElement>(model.JsonAttributes);
                // var userName = jsonAttributes.GetProperty("ContactEmail").GetString();

                // // Check if the user already exists in the database
                // var existingUser = await _userManager.FindByEmailAsync(userName);
                // if (existingUser != null)
                // {
                //     // User already exists, no need to create again
                //     _logger.LogInformation($"User with email {userName} already exists.");
                //     return;
                // }

                // // Extract contact name and split it into first and last name
                // var contactName = jsonAttributes.GetProperty("Contact").GetString();
                // var names = contactName?.Split(' ') ?? new string[] { };

                // var firstName = names.FirstOrDefault() ?? "First Name";
                // var lastName = names.Length > 1 ? names[1] : "Last Name";

                // // Create a new RegisterSystemUserModel
                // var newSubcontractorUser = new RegisterSystemUserModel
                // {
                //     CompanyID = model.SubcontractorID,
                //     FirstName = firstName,
                //     LastName = lastName,
                //     UserName = userName,
                //     isSCTF = true
                // };

                // // Generate a password using predefined rules
                // var password = GeneratePassword();

                // newSubcontractorUser.Password = password;
                // newSubcontractorUser.ConfirmPassword = password;

                // try
                // {
                //     // Register the new user
                //     await newSubcontractorUser.RegisterAsync();
                //     _logger.LogInformation($"New subcontractor user {userName} registered successfully.");
                // }
                // catch (UserCreationException ex)
                // {
                //     _logger.LogError(ex, "Error occurred while creating user.");
                //     throw;
                // }
                Console.WriteLine($"TODO: Register new user for subcontractor {model.SubcontractorId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error registering new subcontractor user: {ex.Message}");
            }
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