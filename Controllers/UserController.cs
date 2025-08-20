using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewVivaApi.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using NewVivaApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
// using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using NewVivaApi.Authentication.Models;

namespace NewVivaApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context )
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            // Get the logged-in user ID from the JWT
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                         ?? User.FindFirst("sub")?.Value;
            Console.WriteLine($"Generated password for user: {userId}");

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found in token.");

             // Pull the user profile from the DB
            var aspNetUser = await _context.Database
                .SqlQuery<AspNetUser>($"SELECT * FROM AspNetUsers WHERE Id = {userId}")
                .FirstOrDefaultAsync();
            
            if (aspNetUser == null)
                return NotFound("User profile not found.");

            UserAccessProfile userAccessProfile = new UserAccessProfile()
            {
                UserName = aspNetUser.UserName,
                UserId = userId,
                ResetPasswordOnLogin = aspNetUser.ResetPasswordOnLoginTf,
            };

            var adminUser = _context.AdminUsers.Any(perm => perm.UserId == userId);
            GeneralContractorUser? genContractor = await _context.GeneralContractorUsers
                .FirstOrDefaultAsync(u => u.UserId == userId);
            SubcontractorUser? subContractor = await _context.SubcontractorUsers
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (adminUser)
            {
                userAccessProfile.UserType = UserType.Viva;
                userAccessProfile.CanApproveTF = true;
            }
            else if (genContractor != null)
            {
                userAccessProfile.UserType = UserType.GeneralContractor;
                userAccessProfile.CanApproveTF = genContractor.CanApproveTf;
                userAccessProfile.GeneralContractorID = genContractor.GeneralContractorId;
            }
            else if (subContractor != null)
            {
                userAccessProfile.UserType = UserType.Subcontractor;
                userAccessProfile.CanApproveTF = false; 
                userAccessProfile.SubcontractorID = subContractor.SubcontractorId;
            }
         
            return Ok(userAccessProfile);
        }
    }
}
