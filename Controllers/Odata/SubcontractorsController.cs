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

namespace NewVivaApi.Controllers.Odata
{
    //[Authorize]
    public class SubcontractorsController : ODataController
    {
        private readonly AppDbContext _context;
        //private readonly MapperInstance _mapper;

        public SubcontractorsController(AppDbContext context)
        {
            _context = context;
            //_mapper = mapper;
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
        public ActionResult<IQueryable<SubcontractorsVw>> Get()
        {
            var model = _context.SubcontractorsVws.OrderBy(s => s.SubcontractorId);
            if (model == null)
                return BadRequest();

            return Ok(model);
        }


        [EnableQuery]
        public ActionResult<SubcontractorsVw> Get([FromRoute] int key)
        {
            var model = _context.SubcontractorsVws.FirstOrDefault(s => s.SubcontractorId == key);
            if (model == null)
                return NotFound();

            return Ok(model);
        }

/*
        public async Task<IActionResult> Post([FromBody] SubcontractorsVw model)
        {
            if (User.Identity.IsServiceUser())
                return BadRequest();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var dbModel = new Subcontractor();
            //_mapper.IMapper.Map(model, dbModel);

            dbModel.JsonAttributes = FinancialSecurityService.protectJsonAttributes(model.JsonAttributes);
            dbModel.CreateDT = dbModel.LastUpdateDT = DateTime.UtcNow;
            dbModel.LastUpdateUser = dbModel.CreatedByUser = User.Identity.Name;

            _context.Subcontractors.Add(dbModel);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(ex.Message);
            }

            model.SubcontractorID = dbModel.SubcontractorID;

            var es = new EmailService();
            es.sendAdminEmailNewSubcontractor(User.Identity.GetUserId(), model.SubcontractorID);

            RegisterNewSubcontractorUser(model);
            return Created(model);
        }

        [HttpPatch]
        public async Task<IActionResult> Patch([FromRoute] int key, [FromBody] Delta<SubcontractorsVw> patch)
        {
            if (User.Identity.IsServiceUser())
                return BadRequest();

            var dbModel = _context.Subcontractors.FirstOrDefault(x => x.SubcontractorID == key && x.DeleteDT == null);
            if (dbModel == null)
                return NotFound();

            var model = new Subcontractors_vw();
            //_mapper.IMapper.Map(dbModel, model);
            patch.Patch(model);
            //_mapper.IMapper.Map(model, dbModel);

            dbModel.JsonAttributes = FinancialSecurityService.protectJsonAttributes(model.JsonAttributes);
            dbModel.LastUpdateDT = DateTime.UtcNow;
            dbModel.LastUpdateUser = User.Identity.Name;

            var validationErrors = dbModel.Validate();
            if (validationErrors.Any())
            {
                foreach (var error in validationErrors)
                    ModelState.AddModelError(string.Empty, error);
                return BadRequest(ModelState);
            }

            await _context.SaveChangesAsync();
            model = _context.Subcontractors_vw.FirstOrDefault(x => x.SubcontractorID == key);
            RegisterNewSubcontractorUser(model);
            return Updated(model);
        }

        public async Task<IActionResult> Delete([FromRoute] int key)
        {
            if (User.Identity.IsServiceUser())
                return BadRequest();

            var model = _context.Subcontractors.FirstOrDefault(x => x.SubcontractorID == key && x.DeleteDT == null);
            if (model == null)
                return NotFound();

            model.DeleteDT = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private void RegisterNewSubcontractorUser(SubcontractorsVw model)
        {
            if (User.Identity.IsServiceUser())
                return;

            JObject attributes = JObject.Parse(model.JsonAttributes);
            string userEmail = attributes["ContactEmail"]?.ToString();

            if (string.IsNullOrEmpty(userEmail))
                return;

            var existingUser = _context.AspNetUsers.FirstOrDefault(u => u.Email == userEmail);
            if (existingUser != null)
                return;

            string contactName = attributes["Contact"]?.ToString();
            var names = contactName?.Split(' ') ?? Array.Empty<string>();

            var registerModel = new RegisterSystemUserModel
            {
                CompanyID = model.SubcontractorID,
                FirstName = names.FirstOrDefault() ?? "First",
                LastName = names.Skip(1).FirstOrDefault() ?? "Last",
                UserName = userEmail,
                isSCTF = true
            };

            var password = PasswordGenerationService.GeneratePassword(new()
            {
                RequireNumber = true,
                RequireSymbol = true,
                MinimumLength = 10,
                MaximumLength = 16
            });

            registerModel.Password = registerModel.ConfirmPassword = password;

            try
            {
                registerModel.Register(User.Identity.GetUserName());
            }
            catch (UserCreationException ex)
            {
                throw;
            }
        }
        */
    }
}