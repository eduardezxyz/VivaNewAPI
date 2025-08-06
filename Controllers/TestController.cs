using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewVivaApi.Data;
using System.Linq;
using System.Threading.Tasks;

namespace NewVivaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TestController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                // Test if we can connect to the database
                var canConnect = await _context.Database.CanConnectAsync();
                
                return Ok(new { 
                    CanConnect = canConnect,
                    Message = canConnect ? "Database connection successful!" : "Cannot connect to database"
                });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { 
                    Error = ex.Message,
                    Message = "Database connection failed"
                });
            }
        }

        [HttpGet("tables")]
        public IActionResult GetAvailableTables()
        {
            try
            {
                // Get all DbSet properties (tables) from the context
                var dbSetProperties = _context.GetType()
                    .GetProperties()
                    .Where(p => p.PropertyType.IsGenericType && 
                               p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                    .Select(p => new { 
                        PropertyName = p.Name,
                        ModelType = p.PropertyType.GetGenericArguments()[0].Name
                    })
                    .ToList();

                return Ok(new {
                    Message = "Available tables/models in your DbContext:",
                    Tables = dbSetProperties
                });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { 
                    Error = ex.Message,
                    Message = "Failed to get table information"
                });
            }
        }

        [HttpGet("sample-data")]
        public IActionResult GetSampleData()
        {
            try
            {
                // Try to get some sample data from any available table
                object result = new { Message = "No data found or no accessible tables" };
                
                // Try to access Projects table if it exists
                var projectsProperty = _context.GetType().GetProperty("Projects");
                if (projectsProperty != null)
                {
                    result = new { 
                        Message = "Found Projects table",
                        TableName = "Projects",
                        Type = projectsProperty.PropertyType.GetGenericArguments()[0].Name
                    };
                }

                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { 
                    Error = ex.Message,
                    Message = "Failed to get sample data"
                });
            }
        }
    }
}