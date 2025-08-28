using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Deltas;
using NewVivaApi.Data;
using NewVivaApi.Models;
using AutoMapper;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using NewVivaApi.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
namespace VivaPayAppAPI.Controllers.OData;

public class ProjectsController : ODataController
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public ProjectsController(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    private IQueryable<ProjectsVw> GetSecureModel()
    {
        if (User.Identity.IsServiceUser())
        {
            return null;
        }

        IQueryable<ProjectsVw> model;

        if (User.Identity.IsVivaUser())
        {
            model = _context.ProjectsVws.OrderBy(proj => proj.ProjectName);
        }
        else if (User.Identity.IsGeneralContractor())
        {
            int generalContractorID = (int)User.Identity.GetGeneralContractorID();
            model = _context.ProjectsVws
                .Where(project => project.GeneralContractorId == generalContractorID)
                .OrderBy(proj => proj.ProjectName);
        }
        else if (User.Identity.IsSubContractor())
        {
            int subContractorID = (int)User.Identity.GetSubcontractorID();

            List<int> subProjList = _context.SubcontractorProjectsVws
                .Where(subProj => subProj.SubcontractorId == subContractorID)
                .Select(subproj => subproj.ProjectId)
                .ToList();

            model = _context.ProjectsVws
                .Where(project => subProjList.Contains(project.ProjectId))
                .OrderBy(proj => proj.ProjectName);
        }
        else
        {
            model = null;
        }

        return model;
    }

    [EnableQuery]
    [HttpGet]
    public ActionResult Get()
    {
        if (User.Identity.IsServiceUser())
        {
            return BadRequest();
        }

        var model = GetSecureModel();
        if (model == null)
        {
            return BadRequest("Access denied or no data available");
        }

        return Ok(model);
    }

    [EnableQuery]
    public ActionResult<ProjectsVw> Get(int key)
    {
        if (User.Identity.IsServiceUser())
        {
            return BadRequest();
        }

        var model = GetSecureModel()?.FirstOrDefault(s => s.ProjectId == key);
        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }

    [HttpGet("getBySubcontractorID")]
    public async Task<IActionResult> GetBySubcontractorID(int subcontractorID)
    {
        try
        {
            // Your logic to get projects by subcontractor ID
            var projects = _context.SubcontractorProjectsVws.Where(p => p.SubcontractorId == subcontractorID);
            return Ok(projects);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPost]
    public async Task<ActionResult<ProjectsVw>> Post([FromBody] ProjectsVw model)
    {
        Console.WriteLine($"POST model {model}");
        Console.WriteLine($"Incoming GeneralContractorID: {model.GeneralContractorId}");

        if (User.Identity.IsServiceUser())
        {
            Console.WriteLine("User.Identity.IsServiceUser() Not working");
            return BadRequest();
        }

        // if (!User.Identity.IsVivaUser())
        // {
        //     if (!User.Identity.CanServiceAccountMakeProjectRecord(model.GeneralContractorId))
        //     {
        //         Console.WriteLine("CanServiceAccountMakeProjectRecord Not working");
        //         return BadRequest();
        //     }
        // }

        if (!ModelState.IsValid)
        {
            Console.WriteLine("!ModelState.IsValid (86) Not working");
            return BadRequest(ModelState);
        }

        var existingContractors = await _context.GeneralContractors.Select(gc => gc.GeneralContractorId).ToListAsync();
        Console.WriteLine($"Existing contractor IDs: {string.Join(", ", existingContractors)}");

        var dbModel = _mapper.Map<Project>(model);

        dbModel.CreateDt = DateTime.UtcNow;
        dbModel.LastUpdateDt = DateTime.UtcNow;
        dbModel.LastUpdateUser = User.Identity.Name;
        dbModel.CreatedByUser = User.Identity.Name;

        // Validate(databaseModel);
        // if (!ModelState.IsValid)
        // {
        //     return BadRequest(ModelState);
        // }

        _context.Projects.Add(dbModel);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException e)
        {
            // Return a more specific error message based on the exception
            if (e.InnerException is SqlException sqlEx)
                Console.WriteLine($"Error: {e}");
            return BadRequest("An error occurred while saving the project");
        }

        var resultModel = _mapper.Map<ProjectsVw>(dbModel);

        return Created(resultModel);
    }

    [HttpPatch]
    public async Task<ActionResult<ProjectsVw>> Patch(int key, [FromBody] ProjectsVw patch)
    {   
        var permCheck = GetSecureModel().Any(s => s.ProjectId == key);
        if (!permCheck)
        {
            return NotFound();
        }

        var dbModel = await _context.Projects.FindAsync(key);
        var createdByUser = dbModel.CreatedByUser;

        if (dbModel == null)
            return NotFound();

        _mapper.Map(patch, dbModel);

        dbModel.LastUpdateDt = DateTime.UtcNow;
        dbModel.LastUpdateUser = User.Identity.Name;
        dbModel.CreatedByUser = createdByUser;

        await _context.SaveChangesAsync();

        var updatedViewModel = _mapper.Map<ProjectsVw>(dbModel);
        return Ok(updatedViewModel);
    }

    [HttpDelete]
    public async Task<ActionResult> Delete(int key)
    {
        var entity = await _context.Projects.FindAsync(key);
        if (entity == null)
        {
            return NotFound();
        }

        _context.Projects.Remove(entity);
        await _context.SaveChangesAsync();

        return Ok();
    }

}

