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
using Microsoft.OData;
using Microsoft.AspNetCore.OData.Query.Validator;
namespace VivaPayAppAPI.Controllers.OData;


[Authorize]
public class ProjectsController : ODataController
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly ODataValidationSettings _validationSettings;


    public ProjectsController(AppDbContext context,
    IMapper mapper,
    ODataValidationSettings validationSettings)
    {
        _context = context;
        _mapper = mapper;
        _validationSettings = validationSettings;

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
            int generalContractorID = (int)User.Identity.GetGeneralContractorId();
            model = _context.ProjectsVws
                .Where(project => project.GeneralContractorId == generalContractorID)
                .OrderBy(proj => proj.ProjectName);
        }
        else if (User.Identity.IsSubContractor())
        {
            int subContractorID = (int)User.Identity.GetSubcontractorId();

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
    public ActionResult Get(ODataQueryOptions<ProjectsVw> queryOptions)
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
    public ActionResult<ProjectsVw> Get(int key, ODataQueryOptions<ProjectsVw> queryOptions)
    {
        if (User.Identity.IsServiceUser())
        {
            return BadRequest();
        }

        try
        {
            queryOptions.Validate(_validationSettings);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }

        var model = GetSecureModel()?.FirstOrDefault(s => s.ProjectId == key);
        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }

    // [HttpGet("getBySubcontractorID")]
    // public async Task<IActionResult> GetBySubcontractorID(int subcontractorID)
    // {
    //     try
    //     {
    //         // Your logic to get projects by subcontractor ID
    //         var projects = _context.SubcontractorProjectsVws.Where(p => p.SubcontractorId == subcontractorID);
    //         return Ok(projects);
    //     }
    //     catch (Exception ex)
    //     {
    //         return StatusCode(500, $"Internal server error: {ex.Message}");
    //     }
    // }

    [HttpPost]
    public async Task<ActionResult<ProjectsVw>> Post([FromBody] ProjectsDTO dto)
    {
        if (User.Identity.IsServiceUser())
        {
            Console.WriteLine("User.Identity.IsServiceUser() Not working");
            return BadRequest();
        }

        var model = ConvertDtoToModel(dto);

        // Validate the incoming model first
        if (model == null)
        {
            Console.WriteLine("Incoming model is null");
            return BadRequest("Model cannot be null");
        }

        // Check if AutoMapper is properly configured and available
        if (_mapper == null)
        {
            Console.WriteLine("AutoMapper is null - DI issue");
            return BadRequest("Mapping service unavailable");
        }


        // if (!User.Identity.IsVivaUser())
        // {
        //     if (!User.Identity.CanServiceAccountMakeProjectRecord(model.GeneralContractorId))
        //     {
        //         Console.WriteLine("CanServiceAccountMakeProjectRecord Not working");
        //         return BadRequest();
        //     }
        // }

        // if (!ModelState.IsValid)
        // {
        //     Console.WriteLine("!ModelState.IsValid (86) Not working");
        //     return BadRequest(ModelState);
        // }
        Console.WriteLine($"Incoming GeneralContractorID: {model.GeneralContractorId}");

        var existingContractors = await _context.GeneralContractors.Select(gc => gc.GeneralContractorId).ToListAsync();
        Console.WriteLine($"Existing contractor IDs: {string.Join(", ", existingContractors)}");

        var dbModel = _mapper.Map<Project>(model);

        if (dbModel == null)
        {
            Console.WriteLine("AutoMapper returned null - check mapping configuration");
            return BadRequest("Failed to map model");
        }

        Console.WriteLine($"Mapping successful. DbModel: ProjectName={dbModel.ProjectName}, GeneralContractorId={dbModel.GeneralContractorId}");

        dbModel.CreateDt = DateTime.UtcNow;
        dbModel.LastUpdateDt = DateTime.UtcNow;
        dbModel.LastUpdateUser = User.Identity.Name;
        dbModel.CreatedByUser = User.Identity.Name;

        TryValidateModel(dbModel);
        Console.WriteLine($"After TryValidateModel: ModelState.IsValid = " + ModelState.IsValid);

        _context.Projects.Add(dbModel);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException e)
        {
            var exceptionFormatter = new DbEntityValidationExceptionFormatter(e);
            return BadRequest(exceptionFormatter.Message);
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

        TryValidateModel(dbModel);
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        await _context.SaveChangesAsync();

        var updatedViewModel = _mapper.Map<ProjectsVw>(dbModel);
        return Ok(updatedViewModel);
    }

    [HttpDelete]
    public async Task<ActionResult> Delete(int key)
    {
        var permCheck = GetSecureModel().Any(s => s.ProjectId == key);
        if (!permCheck)
        {
            return NotFound();
        }

        var entity = await _context.Projects.FindAsync(key);
        if (entity == null)
        {
            return NotFound();
        }

        entity.DeleteDt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok();
    }

    // Helper method to convert DTO to your domain model
    private ProjectsVw ConvertDtoToModel(ProjectsDTO dto)
    {
        // Parse UnpaidBalance from string to decimal
        decimal? unpaidBalance = null;
        if (!string.IsNullOrEmpty(dto.UnpaidBalance) && decimal.TryParse(dto.UnpaidBalance, out decimal balance))
        {
            unpaidBalance = balance;
        }

        return new ProjectsVw
        {
            ProjectName = dto.ProjectName,
            GeneralContractorId = dto.GeneralContractorID,  // Map ID to Id
            StartDt = (DateTimeOffset)dto.StartDT,                          // Map DT to Dt
            StatusId = dto.StatusID,                        // Map ID to Id
            UnpaidBalance = unpaidBalance,
            VivaProjectId = dto.VivaProjectID               // Map ID to Id
        };
    }

}


// Create this DTO class that matches your JSON payload exactly
public class ProjectsDTO
{
    public string ProjectName { get; set; } = string.Empty;
    public int GeneralContractorID { get; set; }  // Matches JSON exactly
    public DateTime? StartDT { get; set; }        // Matches JSON exactly  
    public int StatusID { get; set; }             // Matches JSON exactly
    public string UnpaidBalance { get; set; } = string.Empty; // Handle as string initially
    public string VivaProjectID { get; set; } = string.Empty; // Matches JSON exactly
}

