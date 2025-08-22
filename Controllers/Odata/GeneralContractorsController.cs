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

        public GeneralContractorsController(
            AppDbContext context,
            IMapper mapper,
            EmailService emailService,
            IHttpContextAccessor httpContextAccessor,
            Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager
            )

        {
            _context = context;
            _mapper = mapper;
            _emailService = emailService;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;

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
                .OrderBy(g => g.GeneralContractorId)
                .Select(g => new
                {
                    GeneralContractorID = g.GeneralContractorId,
                    GeneralContractorName = g.GeneralContractorName,
                    VivaGeneralContractorID = g.VivaGeneralContractorId,
                    StatusID = g.StatusId,
                    JsonAttributes = g.JsonAttributes,
                    LogoImage = g.LogoImage,
                    CreatedByUser = g.CreatedByUser,
                    DommainName = g.DommainName,
                    NumSubs = g.NumSubs,
                    Outstanding = g.Outstanding
                });

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

            return Ok(model);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] GeneralContractorsVw model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var dbEntity = _mapper.Map<GeneralContractor>(model);

            //dbEntity.JsonAttributes = FinancialSecurityService.ProtectJsonAttributes(model.JsonAttributes);
            dbEntity.CreateDt = System.DateTime.UtcNow;
            dbEntity.LastUpdateDt = System.DateTime.UtcNow;
            dbEntity.LastUpdateUser = dbEntity.CreatedByUser = User?.Identity?.Name;
            dbEntity.LogoImage = model.LogoImage;
            dbEntity.DommainName = model.DommainName;
            dbEntity.CreatedByUser = "deki@steeleconsult@gmail.com";
            dbEntity.LastUpdateUser = "deki@steeleconsult@gmail.com";

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
            var resultModel = _mapper.Map<GeneralContractor>(dbEntity);

            //subdomain service
            //var subdomainService = new SubdomainService();
            //await subdomainService.AddNewSubdomainAsync(model.DomainName); Line was commented / not active in original code

            var userId = User.Identity.GetUserId();
            var genConId = model.GeneralContractorId;
            Console.WriteLine($"Before email services: userId = {userId},  GenConID = {genConId}");
            _emailService.sendAdminEmailNewGeneralContractor(userId, genConId);

            //Register new user
            await RegisterNewGeneralContractor(model);
            return Created(model);
        }

        [HttpPatch]
        public async Task<IActionResult> Patch(int key, [FromBody] GeneralContractorsVw patch)
        {
            var dbEntity = await _context.GeneralContractors.FindAsync(key);

            if (dbEntity == null)
                return NotFound();

            _mapper.Map(patch, dbEntity);
            dbEntity.GeneralContractorId = key;

            //dbEntity.JsonAttributes = FinancialSecurityService.ProtectJsonAttributes(model.JsonAttributes);
            dbEntity.LastUpdateDt = System.DateTime.UtcNow;
            dbEntity.LastUpdateUser = "deki@steeleconsult.com";
            dbEntity.CreatedByUser = "deki@steeleconsult.com";

            if (!TryValidateModel(dbEntity))
                return BadRequest(ModelState);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException?.Message ?? "No inner exception";
                return BadRequest($"Database error: {ex.Message}. Inner: {innerMessage}");
            }


            var refreshed = await _context.GeneralContractors.FindAsync(key);

            //await _registrationService.RegisterNewGeneralContractorAsync(refreshed);

            return Updated(refreshed);
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

            ApplicationUser existingUser = await _userManager.FindByEmailAsync(userName);
            if (existingUser != null && existingUser.Id != null)
            {
                //User Already exists, don't create them.
                return;
            }

            string contactName = jsonAttributes["ContactName"].ToString();
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

            RegisterSystemUserModel newGeneralContractorUser = new RegisterSystemUserModel(_context, _userManager, _httpContextAccessor)
            {
                UserName = userName,
                FirstName = firstName,
                LastName = lastName,
                //PhoneNumber = phoneNumber,
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

