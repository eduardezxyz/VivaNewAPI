using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using NewVivaApi.Data; // Make sure this points to your AppDbContext namespace
using NewVivaApi.Authentication.Models;
using NewVivaApi.Authentication;
using NewVivaApi.Models;
using Microsoft.AspNet.Identity; // Old Identity v2
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace NewVivaApi.Controllers;

[Route("auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public AuthController(AppDbContext context)
    {
        _dbContext = context;
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
            return BadRequest("Username and password are required.");

        var user = await _dbContext.AspNetUsers
            .FirstOrDefaultAsync(u => u.UserName.ToLower() == model.Username.ToLower());

        if (user == null)
            return Unauthorized("Invalid username or password.");

        // Use the legacy Identity v2 PasswordHasher
        var passwordHasher = new PasswordHasher();
        var verificationResult = passwordHasher.VerifyHashedPassword(user.PasswordHash, model.Password);

        if (verificationResult == PasswordVerificationResult.Success)
        {
            // Return only safe user info
            var userInfo = new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.PhoneNumber
            };

            return Ok(userInfo);
        }

        return Unauthorized("Invalid username or password.");
    }

}
