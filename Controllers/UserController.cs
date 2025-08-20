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
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using NewVivaApi.Authentication.Models;

namespace NewVivaApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly NewVivaApi.Authentication.Models.IdentityDbContext _identityDbContext;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            // // Get the logged-in user ID from the JWT
            // var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            //              ?? User.FindFirst("sub")?.Value;

            // if (string.IsNullOrEmpty(userId))
            //     return Unauthorized("User ID not found in token.");

            // // Pull the user profile from the DB
            // var aspNetUser = await _identityDbContext.Users
            //     .FirstOrDefaultAsync(s => s.Id.ToString() == userId);

            // if (aspNetUser == null)
            //     return NotFound("User profile not found.");

            // UserAccessProfile userAccessProfile = new UserAccessProfile()
            // {
            //     UserName = aspNetUser.UserName,
            //     UserId = userId,
            //     ResetPasswordOnLogin = aspNetUser.ResetPasswordOnLoginTF,
            // };

            // var adminUser = _context.AdminUsers.Any(perm => perm.UserId == userId);

            // if (adminUser)
            // {
            //     userAccessProfile.UserType = UserType.Viva;
            //     userAccessProfile.CanApproveTF = true;
            // }
            // // Build the response (example fields — adjust to match your needs)
            var rex = new
            {
                CanApproveTF = true,
                GeneralContractorID = (int?)null,
                ResetPasswordOnLogin = false,
                SubcontractorID = (int?)null,
                UserId = "eb1d35d7-75a8-4176-996d-7580ddf75f8c", //userId,
                UserName = "thomas.perez@integrait.io",
                UserType = 0
            };
         
            return Ok(rex);
        }
    }
}
