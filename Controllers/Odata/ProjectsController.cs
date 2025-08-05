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

namespace VivaPayAppAPI.Controllers.OData
{
    public class ProjectsController : ODataController
    {
        private readonly AppDbContext _context;

        public ProjectsController(AppDbContext context)
        {
            _context = context;
        }

        // private IQueryable<ProjectsVw> GetSecureModel()
        // {
        //     if (User.Identity.IsServiceUser())
        //         return null;

        //     IQueryable<ProjectsVw> model;

        //     if (User.Identity.IsVivaUser())
        //     {
        //         model = _context.ProjectsVws.OrderBy(p => p.ProjectName);
        //     }
        //     else if (User.Identity.IsGeneralContractor())
        //     {
        //         int generalContractorID = (int)User.Identity.GetGeneralContractorID();
        //         model = _context.ProjectsVws
        //                 .Where(p => p.GeneralContractorID == generalContractorID)
        //                 .OrderBy(p => p.ProjectName);
        //     }
        //     else if (User.Identity.IsSubContractor())
        //     {
        //         int subContractorID = (int)User.Identity.GetSubcontractorID();
        //         var subProjList = _context.SubcontractorProjectsVws
        //                 .Where(sp => sp.SubcontractorID == subContractorID)
        //                 .Select(sp => sp.ProjectID)
        //                 .ToList();

        //         model = _context.ProjectsVws
        //                 .Where(p => subProjList.Contains(p.ProjectID))
        //                 .OrderBy(p => p.ProjectName);
        //     }
        //     else
        //     {
        //         model = null;
        //     }

        //     return model;
        // }

        [EnableQuery]
        public ActionResult<IQueryable<ProjectsVw>> Get()
        {
            var model = _context.ProjectsVws.OrderBy(p => p.ProjectName);
            if (model == null)
                return BadRequest();

            return Ok(model);
        }

        [EnableQuery]
        public ActionResult<ProjectsVw> Get([FromRoute] int key)
        {
            var model = _context.ProjectsVws.FirstOrDefault(p => p.ProjectId == key);
            if (model == null)
                return NotFound();

            return Ok(model);
        }

        // public async Task<ActionResult<ProjectsVw>> Post([FromBody] ProjectsVw model)
        // {
        //     if (User.Identity.IsServiceUser())
        //         return BadRequest();

        //     if (!User.Identity.IsVivaUser() &&
        //         !User.Identity.CanServiceAccountMakeProjectRecord(model.GeneralContractorID))
        //     {
        //         return BadRequest();
        //     }

        //     if (!ModelState.IsValid)
        //         return BadRequest(ModelState);

        //     var dbModel = new Project();
        //     _mapper.IMapper.Map(model, dbModel);

        //     dbModel.CreateDT = DateTime.UtcNow;
        //     dbModel.LastUpdateDT = DateTime.UtcNow;
        //     dbModel.LastUpdateUser = User.Identity.Name;
        //     dbModel.CreatedByUser = User.Identity.Name;

        //     _context.Projects.Add(dbModel);
        //     await _context.SaveChangesAsync();

        //     model.ProjectID = dbModel.ProjectID;

        //     return Created(model);
        // }

    //     [HttpPatch]
    //     [HttpPut]
    //     public async Task<ActionResult> Patch([FromRoute] int key, [FromBody] Delta<ProjectsVw> patch)
    //     {
    //         var exists = GetSecureModel()?.Any(p => p.ProjectID == key) ?? false;
    //         if (!exists)
    //             return NotFound();

    //         var dbModel = await _context.Projects.FindAsync(key);
    //         if (dbModel == null)
    //             return NotFound();

    //         var viewModel = new ProjectsVw();
    //         _mapper.IMapper.Map(dbModel, viewModel);

    //         patch.Patch(viewModel);

    //         _mapper.IMapper.Map(viewModel, dbModel);
    //         dbModel.LastUpdateDT = DateTime.UtcNow;
    //         dbModel.LastUpdateUser = User.Identity.Name;

    //         if (!ModelState.IsValid)
    //             return BadRequest(ModelState);

    //         await _context.SaveChangesAsync();

    //         return NoContent();
    //     }

    //     [HttpDelete]
    //     public async Task<ActionResult> Delete([FromRoute] int key)
    //     {
    //         var exists = GetSecureModel()?.Any(p => p.ProjectID == key) ?? false;
    //         if (!exists)
    //             return NotFound();

    //         var dbModel = await _context.Projects.FindAsync(key);
    //         if (dbModel == null)
    //             return NotFound();

    //         dbModel.DeleteDT = DateTime.UtcNow;
    //         await _context.SaveChangesAsync();

    //         return NoContent();
    //     }
    }
}
