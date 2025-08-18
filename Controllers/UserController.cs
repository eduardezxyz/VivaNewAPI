using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewVivaApi.Data;
using NewVivaApi.Models;
using System.Security.Claims;
using NewVivaApi.Extensions;
using NewVivaApi.Authentication.Models; 

namespace NewVivaApi.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserController> _logger;
        private readonly IdentityDbContext _identityDbContext;

        public UserController(AppDbContext context, ILogger<UserController> logger, IdentityDbContext identityDbContext)
        {
            _context = context;
            _logger = logger;
            _identityDbContext = identityDbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                _logger.LogInformation("UserController.Get() called");
                
                // Debug: Check if User.Identity exists
                _logger.LogInformation("User.Identity is null: {IsNull}", User.Identity == null);
                _logger.LogInformation("User.Identity.IsAuthenticated: {IsAuth}", User.Identity?.IsAuthenticated ?? false);
                
                // Debug: Check service user first
                bool isServiceUser = false;
                try
                {
                    isServiceUser = User.Identity?.IsServiceUser() == true;
                    _logger.LogInformation("IsServiceUser check result: {IsServiceUser}", isServiceUser);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking IsServiceUser");
                }

                if (isServiceUser)
                {
                    return BadRequest("Service users cannot access user profiles");
                }

                string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("UserId from claims: {UserId}", userId ?? "NULL");
                
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("No user ID in claims - checking database for test users");
                    
                    // Count total users
                    var totalUsers = await _identityDbContext.Users.CountAsync();
                    _logger.LogInformation("Total users in AspNetUsers: {Count}", totalUsers);
                    
                    if (totalUsers == 0)
                    {
                        return BadRequest("No users found in AspNetUsers table");
                    }
                    
                    // Get first user for testing
                    var testUser = await _identityDbContext.Users.FirstOrDefaultAsync();
                    if (testUser != null)
                    {
                        userId = testUser.Id;
                        _logger.LogInformation("Using test user - ID: {UserId}, Email: {Email}", userId, testUser.Email);
                    }
                    else
                    {
                        return BadRequest("Failed to get test user from database");
                    }
                }

                // Check if UserProfile exists
                var userProfilesCount = await _context.UserProfiles.CountAsync();
                _logger.LogInformation("Total UserProfiles in database: {Count}", userProfilesCount);
                
                var aspNetUser = await _context.UserProfiles
                    .FirstOrDefaultAsync(s => s.UserId == userId);
                
                if (aspNetUser == null)
                {
                    _logger.LogWarning("UserProfile not found for UserId: {UserId}", userId);
                    
                    // Check if user exists in AspNetUsers
                    var userExistsInAspNet = await _identityDbContext.Users.AnyAsync(u => u.Id == userId);
                    _logger.LogInformation("User exists in AspNetUsers: {Exists}", userExistsInAspNet);
                    
                    if (userExistsInAspNet)
                    {
                        // Get the AspNetUser details
                        var aspNetUserDetails = await _identityDbContext.Users
                            .Where(u => u.Id == userId)
                            .Select(u => new { u.Id, u.Email, u.UserName })
                            .FirstOrDefaultAsync();
                            
                        return BadRequest($"User {aspNetUserDetails?.Email} exists in AspNetUsers but no UserProfile found. You may need to create a UserProfile record.");
                    }
                    
                    return BadRequest($"User with ID {userId} not found in either AspNetUsers or UserProfiles");
                }

                var userAccessProfile = new UserAccessProfile
                {
                    UserName = aspNetUser.UserName,
                    UserId = userId,
                    ResetPasswordOnLogin = false
                };

                // Set user type and permissions based on role
                try
                {
                    if (User.Identity != null && User.Identity.IsVivaUser())
                    {
                        userAccessProfile.UserType = UserType.Viva;
                        userAccessProfile.CanApproveTf = true;
                        _logger.LogInformation("User identified as Viva user");
                    }
                    else if (User.Identity != null && User.Identity.IsGeneralContractor())
                    {
                        userAccessProfile.UserType = UserType.GeneralContractor;
                        userAccessProfile.CanApproveTf = User.Identity.CanApproveTf();
                        userAccessProfile.GeneralContractorId = User.Identity.GetGeneralContractorID();
                        _logger.LogInformation("User identified as General Contractor");
                    }
                    else if (User.Identity != null && User.Identity.IsSubContractor())
                    {
                        userAccessProfile.UserType = UserType.Subcontractor;
                        userAccessProfile.CanApproveTf = false;
                        userAccessProfile.SubcontractorId = User.Identity.GetSubcontractorID();
                        _logger.LogInformation("User identified as Subcontractor");
                    }
                    else
                    {
                        // Default for testing
                        _logger.LogWarning("No specific user type found - defaulting to Viva");
                        userAccessProfile.UserType = UserType.Viva;
                        userAccessProfile.CanApproveTf = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error determining user type, using default");
                    userAccessProfile.UserType = UserType.Viva;
                    userAccessProfile.CanApproveTf = true;
                }
                
                _logger.LogInformation("Successfully retrieved user profile for UserId: {UserId}, UserType: {UserType}", 
                    userId, userAccessProfile.UserType);

                return Ok(userAccessProfile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in UserController.Get()");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("IsServiceUser")]
        public async Task<IActionResult> IsServiceUser([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email is required.");
            }

            try
            {
                // Check if the email belongs to a service user
                var isServiceUser = await _context.ServiceUsers
                    .Join(_identityDbContext.Users,
                          su => su.UserId,
                          au => au.Id,
                          (su, au) => new { ServiceUser = su, AspNetUser = au })
                    .AnyAsync(joined => joined.AspNetUser.Email != null && 
                                       joined.AspNetUser.Email.ToLower() == email.ToLower());

                _logger.LogInformation("Service user check for email: {Email}, Result: {IsServiceUser}", 
                    email, isServiceUser);

                return Ok(isServiceUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if email {Email} is a service user", email);
                return StatusCode(500, "An error occurred while checking service user status");
            }
        }
    }
}using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewVivaApi.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using NewVivaApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;

namespace NewVivaApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            // Get the logged-in user ID from the JWT
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                         ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found in token.");

            // Pull the user profile from the DB
            var aspNetUser = await _context.AspNetUsers
                .FirstOrDefaultAsync(s => s.Id.ToString() == userId);

            if (aspNetUser == null)
                return NotFound("User profile not found.");

            UserAccessProfile userAccessProfile = new UserAccessProfile()
            {
                UserName = aspNetUser.UserName,
                UserId = userId,
                ResetPasswordOnLogin = aspNetUser.ResetPasswordOnLoginTf ?? false,
            };

            var adminUser = _context.AdminUsers.Any(perm => perm.UserId == userId);

            if (adminUser)
            {
                userAccessProfile.UserType = UserType.Viva;
                userAccessProfile.CanApproveTF = true;
            }
            // // Build the response (example fields — adjust to match your needs)
            var rex = new
            {
                CanApproveTF = true,
                GeneralContractorID = (int?)null,
                ResetPasswordOnLogin = false,
                SubcontractorID = (int?)null,
                UserId = userId,// "eb1d35d7-75a8-4176-996d-7580ddf75f8c",
                UserName = "thomas.perez@integrait.io",
                UserType = 1
            };
         
            return Ok(userAccessProfile);
        }
    }
}
