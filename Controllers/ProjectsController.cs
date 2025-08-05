using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewVivaApi.Data;
using NewVivaApi.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewVivaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProjectsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Projects
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
        {
            try
            {
                var projects = await _context.Projects
                    .Include(p => p.GeneralContractor)
                    .Take(10) // Limit to 10 for testing
                    .ToListAsync();

                return Ok(projects);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // GET: api/Projects/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Project>> GetProject(int id)
        {
            try
            {
                var project = await _context.Projects
                    .Include(p => p.GeneralContractor)
                    .FirstOrDefaultAsync(p => p.ProjectId == id);

                if (project == null)
                {
                    return NotFound();
                }

                return Ok(project);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // GET: api/Projects/view
        // This endpoint uses the ProjectsVw view
        [HttpGet("view")]
        public async Task<ActionResult<IEnumerable<ProjectsVw>>> GetProjectsView()
        {
            try
            {
                var projects = await _context.ProjectsVws
                    .Take(10) // Limit to 10 for testing
                    .ToListAsync();

                return Ok(projects);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // GET: api/Projects/view/5
        [HttpGet("view/{id}")]
        public async Task<ActionResult<ProjectsVw>> GetProjectView(int id)
        {
            try
            {
                var project = await _context.ProjectsVws
                    .FirstOrDefaultAsync(p => p.ProjectId == id);

                if (project == null)
                {
                    return NotFound();
                }

                return Ok(project);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // POST: api/Projects
        [HttpPost]
        public async Task<ActionResult<Project>> PostProject(Project project)
        {
            try
            {
                // Set audit fields
                project.CreateDt = DateTime.UtcNow;
                project.LastUpdateDt = DateTime.UtcNow;
                project.LastUpdateUser = "System"; // You'll replace this with actual user
                project.CreatedByUser = "System";

                _context.Projects.Add(project);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetProject), new { id = project.ProjectId }, project);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // PUT: api/Projects/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProject(int id, Project project)
        {
            if (id != project.ProjectId)
            {
                return BadRequest("ID mismatch");
            }

            try
            {
                var existingProject = await _context.Projects.FindAsync(id);
                if (existingProject == null)
                {
                    return NotFound();
                }

                // Update properties
                existingProject.ProjectName = project.ProjectName;
                existingProject.VivaProjectId = project.VivaProjectId;
                existingProject.GeneralContractorId = project.GeneralContractorId;
                existingProject.StatusId = project.StatusId;
                existingProject.StartDt = project.StartDt;
                existingProject.JsonAttributes = project.JsonAttributes;
                existingProject.LastUpdateDt = DateTime.UtcNow;
                existingProject.LastUpdateUser = "System"; // You'll replace this with actual user

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // DELETE: api/Projects/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            try
            {
                var project = await _context.Projects.FindAsync(id);
                if (project == null)
                {
                    return NotFound();
                }

                // Soft delete
                project.DeleteDt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // GET: api/Projects/count
        [HttpGet("count")]
        public async Task<ActionResult<int>> GetProjectsCount()
        {
            try
            {
                var count = await _context.Projects
                    .Where(p => p.DeleteDt == null) // Only count non-deleted projects
                    .CountAsync();

                return Ok(new { Count = count });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}