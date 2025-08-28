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
using NewVivaApi.Data;
using NewVivaApi.Models;
using System.Text.Json;
using AutoMapper;
using NewVivaApi.Authentication;
using NewVivaApi.Extensions;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNet.Identity;
using NewVivaApi.Services;

namespace NewVivaApi.Controllers.Odata
{
    //[Authorize]
    public class GeneralContractorsController : ODataController
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly EmailService _emailService;
        private readonly FinancialSecurityService _financialSecurityService;

        public GeneralContractorsController(
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
            _emailService = emailService;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _financialSecurityService = financialSecurityService;
        }

        // [EnableQuery]
        // private ActionResult<IQueryable<GeneralContractorsVw>> GetSecureModel()
        // {
        //     // TODO
        // }

        [EnableQuery]
        [HttpGet]
        public ActionResult Get()
        {
            var model = _context.GeneralContractorsVws
                .OrderBy(g => g.GeneralContractorId);

            if (!model.Any())
                return BadRequest("No records found.");

            return Ok(model);
        }

        [EnableQuery]
        public ActionResult<GeneralContractorsVw> Get([FromRoute] int key)
        {
            var model = _context.GeneralContractorsVws.FirstOrDefault(g => g.GeneralContractorId == key);
            if (model == null)
                return NotFound();

            //Decryption
            model = _financialSecurityService.GenerateUnprotectedJsonAttributes(model);

            return Ok(model);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] GeneralContractorsVw model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            //Encryption
            string protectedJsonAttributes = _financialSecurityService.ProtectJsonAttributes(model.JsonAttributes);

            var dbEntity = _mapper.Map<GeneralContractor>(model);

            dbEntity.JsonAttributes = protectedJsonAttributes;
            dbEntity.CreateDt = System.DateTime.UtcNow;
            dbEntity.LastUpdateDt = System.DateTime.UtcNow;
            dbEntity.LogoImage = model.LogoImage;
            dbEntity.DommainName = model.DommainName;
            dbEntity.CreatedByUser = User.Identity?.Name; ;
            dbEntity.LastUpdateUser = User.Identity?.Name;

            _context.GeneralContractors.Add(dbEntity);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException?.Message ?? "No inner exception";
                return BadRequest($"Database error: {ex.Message}. Inner: {innerMessage}");
            }

            model.GeneralContractorId = dbEntity.GeneralContractorId;

            //var resultModel = _mapper.Map<GeneralContractor>(dbEntity);

            //subdomain service
            //var subdomainService = new SubdomainService();
            //await subdomainService.AddNewSubdomainAsync(model.DomainName); Line was commented / not active in original code

            //Email
            var userId = User.Identity.GetUserId();
            var genConId = model.GeneralContractorId;
            Console.WriteLine($"Before email services: userId = {userId},  GenConID = {genConId}");
            _emailService.sendAdminEmailNewGeneralContractor(userId, genConId);

            //Register new user
            await RegisterNewGeneralContractor(model);
            return Created(model);
        }

        [HttpPatch("{key}")]
        public async Task<IActionResult> Patch(int key, [FromBody] Delta<GeneralContractorsVw> patch)
        {

            if (User.Identity?.IsServiceUser() == true)
            {
                return BadRequest();
            }
            var databaseModel = await _context.GeneralContractors
                .FirstOrDefaultAsync(s => s.GeneralContractorId == key && s.DeleteDt == null);

            if (databaseModel == null)
                return NotFound();

            var createdByUser = databaseModel.CreatedByUser;
            var model = new GeneralContractorsVw();

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
            databaseModel.LogoImage = model.LogoImage;
            databaseModel.DommainName = model.DommainName;
            databaseModel.CreatedByUser = createdByUser;

            // if (!TryValidateModel(dbEntity))
            //     return BadRequest(ModelState);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException?.Message ?? "No inner exception";
                return BadRequest($"Database error: {ex.Message}. Inner: {innerMessage}");
            }

            var updatedModel = await _context.GeneralContractorsVws.FirstOrDefaultAsync(g => g.GeneralContractorId == key);
            Console.WriteLine($"==========General Contractor PATCH: Updated model=============: {updatedModel}");
            await RegisterNewGeneralContractor(updatedModel);

            return Ok(updatedModel);
        }


        [HttpDelete]
        public async Task<IActionResult> Delete([FromRoute] int key)
        {
            var dbEntity = await _context.GeneralContractors.FindAsync(key);

            if (dbEntity == null)
            {
                Console.WriteLine("Entity not found for deletion.");
                Console.WriteLine($"Key: {key}");
                return NotFound();
            }

            dbEntity.DeleteDt = DateTime.UtcNow;

            try
            {
                int changes = await _context.SaveChangesAsync();
                if (changes == 0)
                {
                    return BadRequest("No changes were made to the database.");
                }
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException?.Message ?? "No inner exception";
                return BadRequest($"Database error: {ex.Message}. Inner: {innerMessage}");
            }

            return NoContent();
        }


        private async Task RegisterNewGeneralContractor(GeneralContractorsVw model)
        {
            if (User.Identity.IsServiceUser())
            {
                return;
            }

            JObject jsonAttributes = JObject.Parse(model.JsonAttributes);
            string userName = jsonAttributes["ContactEmail"].ToString();

            Console.WriteLine($"==========General Contractor POST/PATCH: RegisterNewGeneralContractor model=============: {model}");

            ApplicationUser existingUser = await _userManager.FindByEmailAsync(userName);
            if (existingUser != null && existingUser.Id != null)
            {
                //User Already exists, don't create them.
                Console.WriteLine("already exists");
                return;
            }

            Console.WriteLine($"existing user exists: {existingUser}");

            string contactName = jsonAttributes["ContactName"].ToString();
            string[] names = contactName.Split(' ');

            string firstName = "First Name";
            string lastName = "Last Name";
            string phoneNumber = "Phone Number";

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

            RegisterSystemUserModel newGeneralContractorUser = new RegisterSystemUserModel(_context, _userManager, _emailService, _httpContextAccessor)
            {
                UserName = userName,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber,
                Password = generatedPassword,
                ConfirmPassword = generatedPassword,
                CompanyID = model.GeneralContractorId,
                isAdminTF = false,
                isGCTF = true,
                isSCTF = false,
                gcApproveTF = true
            };

            // Register the user
            var creatorUserName = User?.Identity?.Name ?? string.Empty;
            Console.WriteLine($"Registering user: {userName} by {creatorUserName}");

            await newGeneralContractorUser.RegisterAsync(creatorUserName);
            Console.WriteLine("User registration completed.");

            return;

        }
    }
}

