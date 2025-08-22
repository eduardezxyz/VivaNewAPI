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
    // [Authorize]
    public class SubcontractorProjectsController : ODataController
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        // private readonly IEmailService _emailService;

        public SubcontractorProjectsController(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
            // _emailService = emailService;
        }

        private IQueryable<SubcontractorProjectsVw> GetSecureModel()
        {
            // Temporarily return all SubcontractorProjects - we'll add auth logic later
            /*
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
                int? currentGeneralContractorId = User.Identity.GetGeneralContractorID();
                model = _context.SubcontractorProjectsVws.Where(subcontractorProject =>
                    subcontractorProject.GeneralContractorId == currentGeneralContractorId);
            }
            else if (User.Identity.IsSubContractor())
            {
                int? currentSubcontractorId = User.Identity.GetSubcontractorID();
                model = _context.SubcontractorProjectsVws.Where(subcontractorProject =>
                    subcontractorProject.SubcontractorId == currentSubcontractorId);
            }
            else
            {
                model = null;
            }

            return model;
            */

            // For now, return all SubcontractorProjects
            return _context.SubcontractorProjectsVws.OrderBy(sp => sp.ProjectName);
        }

        [EnableQuery]
        public ActionResult Get()
        {
            // Temporarily comment out auth check
            /*
            if (User.Identity.IsServiceUser())
            {
                return null;
            }
            */

            var model = _context.SubcontractorProjectsVws
                .OrderBy(sp => sp.SubcontractorProjectId);

            if (model == null)
                return BadRequest();

            return Ok(model);
        }

        [EnableQuery]
        public ActionResult<SubcontractorProjectsVw> Get([FromRoute] int key)
        {
            // Temporarily comment out auth check
            /*
            if (User.Identity.IsServiceUser())
            {
                return null;
            }
            */

            var model = _context.SubcontractorProjectsVws.FirstOrDefault(sp => sp.SubcontractorProjectId == key);
            if (model == null)
                return NotFound();

            return Ok(model);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SubcontractorProjectsVw model)
        {
            //auth checks
            /*
            if (User.Identity.IsServiceUser())
            {
                return BadRequest();
            }

            if (!User.Identity.CanServiceAccountMakeSubcontractorProjectsRecord(model.SubcontractorId, model.ProjectId))
            {
                return BadRequest();
            }
            */

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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
            dbModel.LastUpdateUser = "testuser@steeleconsult.com";
            dbModel.CreatedByUser = "testuser@steeleconsult.com";

            _context.SubcontractorProjects.Add(dbModel);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException?.Message ?? "No inner exception";
                return BadRequest($"Database error: {ex.Message}. Inner: {innerMessage}");
            }

            var resultModel = _mapper.Map<SubcontractorProjectsVw>(dbModel);

            // TODO: Add email service back
            /*
            var scp = await _context.SubcontractorProjectsVws
                .FirstOrDefaultAsync(l => l.SubcontractorProjectId == dbModel.SubcontractorProjectId);
            
            if (scp != null)
            {
                await _emailService.SendSCAddedToProject(User.Identity.GetUserId(), 
                    scp.SubcontractorId, scp.ProjectName);
            }
            */

            return Created(resultModel);
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

            Console.WriteLine($"Patch SubcontractorProject with ID: {key}");
            Console.WriteLine($"Patch Data: {JsonSerializer.Serialize(patch)}");
            Console.WriteLine($"Current DB Model: SubcontractorId={dbModel.SubcontractorId}, ProjectId={dbModel.ProjectId}, DiscountPct={dbModel.DiscountPct}, StatusId={dbModel.StatusId}");

            var createdByUser = dbModel.CreatedByUser;

            // MANUAL MAPPING: Only update fields that are explicitly provided and not default values

            // Update DiscountPct if provided (allow 0.0 as a valid value)
            if (patch.DiscountPct != default(decimal))
            {
                Console.WriteLine($"Updating DiscountPct from {dbModel.DiscountPct} to {patch.DiscountPct}");
                dbModel.DiscountPct = patch.DiscountPct;
            }

            // Update StatusId if provided (don't update if 0, as that might be unintended)
            if (patch.StatusId != default(int))
            {
                Console.WriteLine($"Updating StatusId from {dbModel.StatusId} to {patch.StatusId}");
                dbModel.StatusId = patch.StatusId;
            }

            // Update JsonAttributes if provided
            if (!string.IsNullOrEmpty(patch.JsonAttributes))
            {
                Console.WriteLine($"Updating JsonAttributes from '{dbModel.JsonAttributes}' to '{patch.JsonAttributes}'");
                dbModel.JsonAttributes = patch.JsonAttributes;
            }

            // NEVER UPDATE THESE (to avoid foreign key issues):
            // - SubcontractorId (would break FK constraint)
            // - ProjectId (would break FK constraint)  
            // - SubcontractorProjectId (primary key, shouldn't change)

            // Update audit fields
            dbModel.LastUpdateDt = DateTimeOffset.UtcNow;
            dbModel.LastUpdateUser = "system@api.com";
            dbModel.CreatedByUser = createdByUser;

            Console.WriteLine($"Final DB Model: SubcontractorId={dbModel.SubcontractorId}, ProjectId={dbModel.ProjectId}, DiscountPct={dbModel.DiscountPct}, StatusId={dbModel.StatusId}");

            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine("Successfully saved changes to database");
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException?.Message ?? "No inner exception";
                Console.WriteLine($"Database error: {ex.Message}. Inner: {innerMessage}");
                return BadRequest($"Database error: {ex.Message}. Inner: {innerMessage}");
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

            Console.WriteLine($"Updated SubcontractorProject: {JsonSerializer.Serialize(updatedViewModel)}");
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
                var innerMessage = ex.InnerException?.Message ?? "No inner exception";
                return BadRequest($"Database error: {ex.Message}. Inner: {innerMessage}");
            }

            return NoContent();
        }
    }
}