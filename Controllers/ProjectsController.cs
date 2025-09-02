using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon;
using Microsoft.AspNetCore.OData.Query;
using NewVivaApi.Models; // Updated namespace
using NewVivaApi.Services; // Updated namespace
using NewVivaApi.Extensions; // For identity extensions
using NewVivaApi.Data; // For DbContext

namespace NewVivaApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public ProjectController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("getBySubcontractorID")]
        public async Task<IActionResult> GetBySubcontractorID(int subcontractorID = 0)
        {
            if (User.Identity.IsServiceUser())
            {
                return BadRequest();
            }

            IQueryable<ProjectsVw> model;

            List<int> excludeProjList = new List<int>();

            if (subcontractorID > 0)
            {
                excludeProjList = await _context.SubcontractorProjectsVws
                                            .Where(subProj => subProj.SubcontractorId == subcontractorID)
                                            .Select(subproj => subproj.ProjectId)
                                            .ToListAsync();
            }

            if (User.Identity.IsVivaUser())
            {
                model = _context.ProjectsVws
                    .Where(p => !excludeProjList.Contains(p.ProjectId))
                    .OrderBy(proj => proj.ProjectName);
            }
            else if (User.Identity.IsGeneralContractor())
            {
                int generalContractorID = (int)User.Identity.GetGeneralContractorId();
                model = _context.ProjectsVws
                    .Where(project => project.GeneralContractorId == generalContractorID
                                   && !excludeProjList.Contains(project.ProjectId))
                    .OrderBy(proj => proj.ProjectName);
            }
            else if (User.Identity.IsSubContractor())
            {
                int subContractorID = (int)User.Identity.GetSubcontractorId();

                List<int> subProjList = await _context.SubcontractorProjectsVws
                                            .Where(subProj => subProj.SubcontractorId == subContractorID)
                                            .Select(subproj => subproj.ProjectId)
                                            .ToListAsync();

                model = _context.ProjectsVws
                    .Where(project => subProjList.Contains(project.ProjectId) 
                                   && !excludeProjList.Contains(project.ProjectId))
                    .OrderBy(proj => proj.ProjectName);
            }
            else
            {
                model = null;
            }

            if (model == null)
            {
                return Ok(new List<ProjectsVw>());
            }

            var result = await model.ToListAsync();
            return Ok(result);
        }

        // protected override void Dispose(bool disposing)
        // {
        //     if (disposing)
        //     {
        //         _context?.Dispose();
        //     }
        //     base.Dispose(disposing);
        // }
    }
}