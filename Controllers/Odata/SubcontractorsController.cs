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
using NewVivaApi.Services;
using Microsoft.AspNet.Identity;

namespace NewVivaApi.Controllers.Odata
{
    [Authorize]
    public class SubcontractorsController : ODataController
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly EmailService _emailService;
        private readonly FinancialSecurityService _financialSecurityService;

        public SubcontractorsController(
            AppDbContext context,
            IMapper mapper,
            EmailService emailService,
            IHttpContextAccessor httpContextAccessor,
            Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager,
            FinancialSecurityService financialSecurityService
            )
        {
            _context = context;
            _mapper = mapper;
            _userManager = userManager;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
            _financialSecurityService = financialSecurityService;
        }
        private IQueryable<SubcontractorsVw> GetSecureModel()
        {
            if (User.Identity.IsServiceUser())
            {
                return null;
            }

            IQueryable<SubcontractorsVw> model;

            if (User.Identity.IsVivaUser())
            {
                model = _context.SubcontractorsVws.OrderBy(subcon => subcon.SubcontractorName);
            }
            else if (User.Identity.IsGeneralContractor())
            {
                int generalContractorId = (int)User.Identity.GetGeneralContractorId();

                List<int> subProjList = _context.SubcontractorProjectsVws
                                            .Where(subProj => subProj.GeneralContractorId == generalContractorId)
                                            .Select(subproj => subproj.SubcontractorId)
                                            .ToList();

                model = _context.SubcontractorsVws
                    .Where(subCon => subProjList.Contains((int)subCon.SubcontractorId))
                    .OrderBy(subcon => subcon.SubcontractorName);

            }
            else if (User.Identity.IsSubContractor())
            {
                int subContractorId = (int)User.Identity.GetSubcontractorId();
                model = _context.SubcontractorsVws.Where(subCon => subCon.SubcontractorId == subContractorId);
            }
            else
            {
                Console.WriteLine($"GetSecureModel() - model is null");
                model = null;
            }

            return model;
        }

        [EnableQuery]
        public ActionResult Get()
        {
            if (User.Identity.IsServiceUser())
            {
                return BadRequest();
            }

            var model = GetSecureModel();

            if (model == null)
                return BadRequest();

            return Ok(model);
        }

        [EnableQuery]
        public ActionResult<SubcontractorsVw> Get([FromRoute] int key)
        {
            if (User.Identity.IsServiceUser())
            {
                return BadRequest();
            }

            var model = GetSecureModel()
                .Where(s => s.SubcontractorId == key)
                .FirstOrDefault();
            if (model == null)
                return NotFound();

            model.JsonAttributes = _financialSecurityService.GenerateUnprotectedJsonAttributes(model.JsonAttributes);
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

            dbModel.JsonAttributes = _financialSecurityService.ProtectJsonAttributes(model.JsonAttributes);
            dbModel.CreateDt = DateTime.UtcNow;
            dbModel.LastUpdateDt = DateTime.UtcNow;
            dbModel.LastUpdateUser = User.Identity.Name;
            dbModel.CreatedByUser = User.Identity.Name;

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

            var userId = User.Identity.GetUserId();
            var genConId = model.SubcontractorId;
            _emailService.sendAdminEmailNewSubcontractor(User.Identity.GetUserId(), model.SubcontractorId);

            await RegisterNewSubcontractorUser(resultModel);
            return Created(resultModel);
        }

        public async Task RegisterNewSubcontractorUser(SubcontractorsVw model)
        {
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

            RegisterSystemUserModel newSubContractorUser = new RegisterSystemUserModel(_context, _userManager, _emailService, _httpContextAccessor)
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
            await newSubContractorUser.RegisterAsync(creatorUserName);

            return;
        }

        [HttpPatch]
        public async Task<IActionResult> Patch(int key, [FromBody] Delta<SubcontractorsVw> patch)
        {
            if (User.Identity?.IsServiceUser() == true)
            {
                return BadRequest();
            }

            var dbModel = await _context.Subcontractors.FindAsync(key);
            var databaseModel = await _context.Subcontractors
                .FirstOrDefaultAsync(s => s.SubcontractorId == key && s.DeleteDt == null);

            if (dbModel == null)
                return NotFound();

            var createdByUser = databaseModel.CreatedByUser;
            var model = new SubcontractorsVw();

            _mapper.Map(databaseModel, model);
            patch.Patch(model);
            _mapper.Map(model, databaseModel);

            // Encryption
            string protectedJsonAttributes = _financialSecurityService.ProtectJsonAttributes(model.JsonAttributes ?? string.Empty);

            var currentUser = User.Identity?.Name;
            var currentTime = DateTime.UtcNow;

            databaseModel.JsonAttributes = protectedJsonAttributes;
            databaseModel.LastUpdateDt = currentTime;
            databaseModel.LastUpdateUser = currentUser;
            databaseModel.CreatedByUser = createdByUser;

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