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
using Microsoft.AspNet.Identity;
using NewVivaApi.Services;
using NewVivaApi.Extensions;
using Microsoft.AspNetCore.OData.Query.Validator;

namespace NewVivaApi.Controllers.Odata
{
    // [Authorize]
    public class SubcontractorProjectsController : ODataController
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly EmailService _emailService;
        private readonly ODataValidationSettings _validationSettings;

        public SubcontractorProjectsController(AppDbContext context, IMapper mapper,
        EmailService emailService, ODataValidationSettings validationSettings)
        {
            _context = context;
            _mapper = mapper;
            _emailService = emailService;
            _validationSettings = validationSettings;

        }

        private IQueryable<SubcontractorProjectsVw> GetSecureModel()
        {
            if (User.Identity.IsServiceUser())
            {
                return null;
            }

            IQueryable<SubcontractorProjectsVw> model;

            if (User.Identity.IsVivaUser())
            {
                model = _context.SubcontractorProjectsVws;
            }
            else if (User.Identity.IsGeneralContractor())
            {
                int? currentGeneralContractorId = User.Identity.GetGeneralContractorId();
                model = _context.SubcontractorProjectsVws
                    .Where(subcontractorProject => subcontractorProject.GeneralContractorId == currentGeneralContractorId);
            }
            else if (User.Identity.IsSubContractor())
            {
                int? currentSubcontractorId = User.Identity.GetSubcontractorId();
                model = _context.SubcontractorProjectsVws
                    .Where(subcontractorProject => subcontractorProject.SubcontractorId == currentSubcontractorId);
            }
            else
            {
                model = null;
            }

            return model;
        }

        [EnableQuery]
        public ActionResult Get(ODataQueryOptions<SubcontractorProjectsVw> queryOptions)
        {
            if (User.Identity.IsServiceUser())
            {
                return BadRequest();
            }

            try
            {
                queryOptions.Validate(_validationSettings);

            }
            catch (ODataException ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok(GetSecureModel());
        }

        [EnableQuery]
        public ActionResult<SubcontractorProjectsVw> Get([FromRoute] int key, ODataQueryOptions<SubcontractorProjectsVw> queryOptions)
        {
            if (User.Identity.IsServiceUser())
            {
                return null;
            }

            try
            {
                queryOptions.Validate(_validationSettings);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

            var model = GetSecureModel().FirstOrDefault(sp => sp.SubcontractorProjectId == key);
            if (model == null)
                return NotFound();

            return Ok(model);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SubcontractorProjectsVw model)
        {
            if (User.Identity.IsServiceUser())
            {
                return BadRequest();
            }
/*
            if (!User.Identity.CanServiceAccountMakeSubcontractorProjectsRecord(model.SubcontractorId, model.ProjectId))
            {
                return BadRequest();
            }
            */
            // Set ProjectName and SubcontractorName to empty or fetch from database
            if (string.IsNullOrEmpty(model.ProjectName))
            {
                var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == model.ProjectId);
                model.ProjectName = project?.ProjectName ?? "Unknown Project";
            }

            if (string.IsNullOrEmpty(model.SubcontractorName))
            {
                var subcontractor = await _context.Subcontractors.FirstOrDefaultAsync(s => s.SubcontractorId == model.SubcontractorId);
                model.SubcontractorName = subcontractor?.SubcontractorName ?? "Unknown Subcontractor";
            }

            // Console.WriteLine($"Model: {model}");
            // if (!ModelState.IsValid)
            // {
            //     return BadRequest(ModelState);
            // }

            // Validate that Subcontractor and Project exist
            var subcontractorExists = await _context.Subcontractors
                .AnyAsync(s => s.SubcontractorId == model.SubcontractorId && s.DeleteDt == null);
            var projectExists = await _context.Projects
                .AnyAsync(p => p.ProjectId == model.ProjectId && p.DeleteDt == null);

            if (!subcontractorExists)
            {
                return BadRequest($"Subcontractor with ID {model.SubcontractorId} does not exist.");
            }
            if (!projectExists)
            {
                return BadRequest($"Project with ID {model.ProjectId} does not exist.");
            }

            var dbModel = _mapper.Map<SubcontractorProject>(model);

            dbModel.CreateDt = DateTimeOffset.UtcNow;
            dbModel.LastUpdateDt = DateTimeOffset.UtcNow;
            dbModel.LastUpdateUser = User.Identity.Name;
            dbModel.CreatedByUser = User.Identity.Name;

            _context.SubcontractorProjects.Add(dbModel);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var exceptionFormatter = new DbEntityValidationExceptionFormatter(ex);
                return BadRequest(exceptionFormatter.Message);
            }

            var resultModel = _mapper.Map<SubcontractorProjectsVw>(dbModel);

            // TODO: Add email service back
            var scp = await _context.SubcontractorProjectsVws
                .FirstOrDefaultAsync(l => l.SubcontractorProjectId == dbModel.SubcontractorProjectId);

            if (scp != null)
            {
                var userId = User.Identity.GetUserId();
                var genConId = model.SubcontractorId;
                await _emailService.sendSCAddedToProject(userId, scp.SubcontractorId, scp.ProjectName);
                return Created(scp);
            }

            return StatusCode(500, "Created record but couldn't retrieve it");
        }



        [HttpPatch]
        public async Task<IActionResult> Patch(int key, [FromBody] SubcontractorProjectsVw patch)
        {
            // Temporarily comment out auth check
            /*
            if (User.Identity.IsServiceUser())
            {
                return BadRequest();
            }
            */
            if (patch == null)
                return BadRequest("No patch data provided");

            var dbModel = await _context.SubcontractorProjects
                .FirstOrDefaultAsync(s => s.SubcontractorProjectId == key && s.DeleteDt == null);

            if (dbModel == null)
                return NotFound();

            var createdByUser = dbModel.CreatedByUser;

            // MANUAL MAPPING: Only update fields that are explicitly provided and not default values

            // Update DiscountPct if provided (allow 0.0 as a valid value)
            if (patch.DiscountPct != default(decimal))
            {
                dbModel.DiscountPct = patch.DiscountPct;
            }

            // Update StatusId if provided (don't update if 0, as that might be unintended)
            if (patch.StatusId != default(int))
            {
                dbModel.StatusId = patch.StatusId;
            }

            // Update JsonAttributes if provided
            if (!string.IsNullOrEmpty(patch.JsonAttributes))
            {
                dbModel.JsonAttributes = patch.JsonAttributes;
            }

            // NEVER UPDATE THESE (to avoid foreign key issues):
            // - SubcontractorId (would break FK constraint)
            // - ProjectId (would break FK constraint)  
            // - SubcontractorProjectId (primary key, shouldn't change)

            // Update audit fields
            dbModel.LastUpdateDt = DateTimeOffset.UtcNow;
            dbModel.LastUpdateUser = User.Identity.Name;
            dbModel.CreatedByUser = createdByUser;


            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var exceptionFormatter = new DbEntityValidationExceptionFormatter(ex);
                return BadRequest(exceptionFormatter.Message);
            }

            // Read the updated record from the view to get complete data
            var updatedViewModel = await _context.SubcontractorProjectsVws
                .FirstOrDefaultAsync(sp => sp.SubcontractorProjectId == key);

            if (updatedViewModel == null)
            {
                Console.WriteLine("Warning: Updated record not found in view");
                // Fallback: create a basic response
                updatedViewModel = new SubcontractorProjectsVw
                {
                    SubcontractorProjectId = dbModel.SubcontractorProjectId,
                    SubcontractorId = dbModel.SubcontractorId,
                    ProjectId = dbModel.ProjectId,
                    DiscountPct = dbModel.DiscountPct,
                    StatusId = dbModel.StatusId,
                    JsonAttributes = dbModel.JsonAttributes
                };
            }

            return Updated(updatedViewModel);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int key)
        {
            // Temporarily comment out auth check
            /*
            if (User.Identity.IsServiceUser())
            {
                return BadRequest();
            }
            */

            var model = await _context.SubcontractorProjects.FindAsync(key);
            if (model == null)
                return NotFound();

            model.DeleteDt = DateTimeOffset.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var exceptionFormatter = new DbEntityValidationExceptionFormatter(ex);
                return BadRequest(exceptionFormatter.Message);
            }

            return NoContent();
        }
    }
}