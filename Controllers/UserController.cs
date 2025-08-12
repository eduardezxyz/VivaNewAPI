using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewVivaApi.Data;
using NewVivaApi.Models;
using System.Security.Claims;
using NewVivaApi.Extensions;

namespace NewVivaApi.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserController> _logger;

        public UserController(AppDbContext context, ILogger<UserController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            if (User.Identity?.IsServiceUser() == true)
            {
                return BadRequest("Service users cannot access user profiles");
            }

            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID not found in claims");
            }

            var aspNetUser = await _context.UserProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId);
            
            if (aspNetUser == null)
            {
                _logger.LogWarning("UserProfile not found for UserId: {UserId}", userId);
                return BadRequest("User profile not found");
            }

            var userAccessProfile = new UserAccessProfile
            {
                UserName = aspNetUser.UserName,
                UserId = userId,
                ResetPasswordOnLogin = false
            };

            // Set user type and permissions based on role
            if (User.Identity != null && User.Identity.IsVivaUser())
            {
                userAccessProfile.UserType = UserType.Viva;
                userAccessProfile.CanApproveTf = true;
            }
            else if (User.Identity != null && User.Identity.IsGeneralContractor())
            {
                userAccessProfile.UserType = UserType.GeneralContractor;
                userAccessProfile.CanApproveTf = User.Identity.CanApproveTf();
                userAccessProfile.GeneralContractorId = User.Identity.GetGeneralContractorID();
            }
            else if (User.Identity != null && User.Identity.IsSubContractor())
            {
                userAccessProfile.UserType = UserType.Subcontractor;
                userAccessProfile.CanApproveTf = false;
                userAccessProfile.SubcontractorId = User.Identity.GetSubcontractorID();
            }
            
            _logger.LogInformation("User profile retrieved for UserId: {UserId}, UserType: {UserType}", 
                userId, userAccessProfile.UserType);

            return Ok(userAccessProfile);
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
                    .Join(_context.AspNetUsers,
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
}