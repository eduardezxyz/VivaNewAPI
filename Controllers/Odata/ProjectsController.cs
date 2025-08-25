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

    [EnableQuery]
    [HttpGet]
    public ActionResult Get()
    {
        var model = _context.ProjectsVws;

        if (!model.Any())
            return BadRequest("No records found.");

        return Ok(model);
    }

    [EnableQuery]
    public ActionResult<ProjectsVw> Get(int key) 
    {
        var model = _context.ProjectsVws.FirstOrDefault(p => p.ProjectId == key);
        if (model == null)
            return NotFound();

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
        var dbModel = _mapper.Map<Project>(model);

        dbModel.CreateDt = DateTime.UtcNow;
        dbModel.LastUpdateDt = DateTime.UtcNow;
        dbModel.LastUpdateUser = "eduard@steeleconsult.com";
        dbModel.CreatedByUser = "eduard@steeleconsult.com";

        _context.Projects.Add(dbModel);
        await _context.SaveChangesAsync();

        var resultModel = _mapper.Map<ProjectsVw>(dbModel);

        return Created(resultModel);
    }

    [HttpPatch]
    public async Task<ActionResult<ProjectsVw>> Patch(int key, [FromBody] ProjectsVw patch)
    {
        var dbModel = await _context.Projects.FindAsync(key);
        if (dbModel == null)
            return NotFound();

        _mapper.Map(patch, dbModel);

        dbModel.LastUpdateDt = DateTime.UtcNow;
        dbModel.LastUpdateUser = "eduard@steeleconsult.com";

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

