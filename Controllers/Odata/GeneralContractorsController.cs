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
using System.Text.Json;
using AutoMapper;

namespace NewVivaApi.Controllers.Odata
{
    //[Authorize]
    public class GeneralContractorsController : ODataController
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        //private readonly UserRegistrationService _registrationService;

        public GeneralContractorsController(
            AppDbContext context,
            IMapper mapper
            ) //UserRegistrationService registrationService
        {
            _context = context;
            _mapper = mapper;
            //_registrationService = registrationService;
        }

        // [EnableQuery]
        // private ActionResult<IQueryable<GeneralContractorsVw>> GetSecureModel()
        // {
        //     // TODO
        // }

        [EnableQuery]
        public ActionResult<IQueryable<GeneralContractorsVw>> Get()
        {
            var model = _context.GeneralContractorsVws.OrderBy(g => g.GeneralContractorId);
            if (model == null)
                return BadRequest();

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
            //await subdomainService.AddNewSubdomainAsync(model.DomainName);

            // var emailSvc = new EmailService();
            // emailSvc.SendAdminEmailNewGeneralContractor(User.Identity.GetUserId(), model.GeneralContractorId);

            //await _registrationService.RegisterNewGeneralContractorAsync(model);

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

    }
}
