using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using NewVivaApi.Data; // Make sure this points to your AppDbContext namespace
using NewVivaApi.Authentication.Models;
using NewVivaApi.Authentication;
using NewVivaApi.Models;
using Microsoft.AspNet.Identity; // Old Identity v2
// using Microsoft.AspNetCore.Identity; // Use this instead (new Identity)
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using NewVivaApi.Authentication.Models;

namespace NewVivaApi.Authentication;

[Route("auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly AuthService _service;
    private readonly IdentityDbContext _identityDbContext;

    public AuthController(AppDbContext context, AuthService service, IdentityDbContext identityDbContext)
    {
        _dbContext = context;
        _service = service;
        _identityDbContext = identityDbContext;
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        Console.WriteLine("=== LOGIN DEBUG ===");
        if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
            return BadRequest("Username and password are required.");

        var normalizedUsername = model.Username.ToUpperInvariant();
        Console.WriteLine($"Attempting to log in user: {normalizedUsername}");
        var user = await _identityDbContext.Users
            .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUsername);

        Console.WriteLine($"User found: {user != null}");
        if (user == null)
            return Unauthorized("Invalid username or password.");

        Console.WriteLine($"User ID: {user.Id}, UserName: {user.UserName}");

        // DEBUG: Log password details
        Console.WriteLine($"Input password: '{model.Password}'");
        Console.WriteLine($"Input password length: {model.Password.Length}");
        Console.WriteLine($"Stored hash: {user.PasswordHash}");
        Console.WriteLine($"Hash length: {user.PasswordHash?.Length}");
        Console.WriteLine($"Hash starts with: {user.PasswordHash?.Substring(0, Math.Min(20, user.PasswordHash.Length))}...");

        // Use the legacy Identity v2 PasswordHasher
        // var passwordHasher = new PasswordHasher();
        // var verificationResult = passwordHasher.VerifyHashedPassword(user.PasswordHash, model.Password);

        //Check password using ASP.NET Core Identity's PasswordHasher
        var verificationResult = VerifyPasswordWithDetection(user, model.Password);

        Console.WriteLine($"Password verification result: {verificationResult}");

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
            Console.WriteLine($"User logged in successfully: {userInfo.UserName}");

            return Ok(userInfo);
        }

        return Unauthorized("Invalid username or password.");
    }

    // [HttpPost("login")]
    // public async Task<IActionResult> Login([FromBody] JsonElement data)
    // {
    //     Console.WriteLine("=== LOGIN DEBUG ===");

    //     try
    //     {
    //         // Extract username and password from JSON
    //         string username = "";
    //         string password = "";

    //         if (data.TryGetProperty("username", out JsonElement usernameElement))
    //             username = usernameElement.GetString() ?? "";

    //         if (data.TryGetProperty("password", out JsonElement passwordElement))
    //             password = passwordElement.GetString() ?? "";

    //         Console.WriteLine($"Login attempt for: {username}");

    //         // Validate input
    //         if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
    //         {
    //             return BadRequest(new { Type = "error", Message = "Username and password are required." });
    //         }

    //         var login = new LoginModel(username, password);

    //         // Use AuthService (which uses UserManager properly)
    //         var result = await _service.Login(login);

    //         if (result == null)
    //         {
    //             return Unauthorized(new { Type = "error", Message = "Invalid username or password." });
    //         }

    //         return Ok(new { Type = "success", Token = result });
    //     }
    //     catch (Exception ex)
    //     {
    //         Console.WriteLine($"Login error: {ex.Message}");
    //         return StatusCode(500, new { Type = "error", Message = "Internal server error" });
    //     }
    // }

    [HttpPost("Register")]
    public async Task<ActionResult> RegisterSystemUser([FromBody] JsonElement data)
    {
        Console.WriteLine("=== REGISTER SYSTEM USER ===");

        try
        {
            // Extract data from JSON (avoiding model binding issues)
            var model = ExtractRegisterModel(data);

            // Manual validation
            var validationErrors = ValidateRegisterModel(model);
            if (validationErrors.Any())
            {
                return BadRequest(new { errors = validationErrors });
            }

            // Generate password
            var requirements = new PasswordRequirements
            {
                RequireNumber = true,
                RequireSymbol = true,
                RequireLowercase = true,
                RequireUppercase = true,
                MinimumLength = 10,
                MaximumLength = 16
            };

            string generatedPassword = PasswordGenerationService.GeneratePassword(requirements);
            model.Password = generatedPassword;
            model.ConfirmPassword = generatedPassword;

            Console.WriteLine($"Generated password for user: {model.Email}");

            // Register the user
            var result = await _service.RegisterSystemUser(model);
            Console.WriteLine($"User registration result: {result?.Id}");

            if (result == null)
            {
                return BadRequest(new { Type = "error", Message = "User registration failed!" });
            }

            return Ok(new
            {
                Type = "success",
                Message = "User registered successfully!",
                UserId = result.Id,
                GeneratedPassword = generatedPassword // You might want to remove this in production
            });
        }
        catch (UserCreationException uce)
        {
            Console.WriteLine($"User creation failed: {string.Join(", ", uce.IdentityResult.Errors.Select(e => e.Description))}");

            // Check for duplicate email error
            foreach (var error in uce.IdentityResult.Errors)
            {
                if (error.Code == "DuplicateEmail" || error.Description.Contains("already exists"))
                {
                    return BadRequest(new { Type = "error", Message = "Email already exists" });
                }
            }

            return BadRequest(new
            {
                Type = "error",
                Message = "User registration failed",
                Errors = uce.IdentityResult.Errors.Select(e => e.Description)
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Registration error: {ex.Message}");
            return StatusCode(500, new { Type = "error", Message = "Internal server error" });
        }
    }

    // Helper methods
    private RegisterSystemUserModel ExtractRegisterModel(JsonElement data)
    {
        return new RegisterSystemUserModel
        {
            FirstName = GetJsonProperty(data, "firstName"),
            LastName = GetJsonProperty(data, "lastName"),
            Email = GetJsonProperty(data, "email"),
            CompanyName = GetJsonProperty(data, "companyName"),
            JobTitle = GetJsonProperty(data, "jobTitle"),
            PhoneNumber = GetJsonProperty(data, "phoneNumber")
        };
    }

    private string GetJsonProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) ? prop.GetString() ?? "" : "";
    }

    private List<string> ValidateRegisterModel(RegisterSystemUserModel model)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(model.FirstName))
            errors.Add("First name is required");

        if (string.IsNullOrWhiteSpace(model.LastName))
            errors.Add("Last name is required");

        if (string.IsNullOrWhiteSpace(model.Email))
            errors.Add("Email is required");
        else if (!IsValidEmail(model.Email))
            errors.Add("Invalid email format");

        return errors;
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }


    // [HttpGet("Refresh")]
    // [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    // public async Task<ActionResult> TokenRefresh()
    // {
    //     var newToken = await _service.TokenRefresh();
    //     return Ok(new DataResponse<string> { Type = "success", Message = "New token created successfully!", Data = newToken});
    // }

    // [HttpGet("Impersonate")]
    // [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin")]
    // public async Task<ActionResult> Impersonate([FromQuery] string id){
    //     var impersonationToken = await _service.Impersonate(id);
    //     if(impersonationToken == null){
    //         return BadRequest(new Response { Type = "error", Message = "Error creating impersonation token!"});
    //     }
    //     return Ok(new DataResponse<string> { Type = "success", Message = "Impersonation token created successfully!", Data = impersonationToken });
    // }
    
    private PasswordVerificationResult VerifyPasswordWithDetection(ApplicationUser user, string password)
    {
        // Detect hash format
        if (user.PasswordHash.StartsWith("AQAAAA")) // Modern ASP.NET Core format
        {
            Console.WriteLine("Detected modern hash format");
            var modernHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<ApplicationUser>();
            return (PasswordVerificationResult)modernHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        }
        else // Legacy format (usually base64 without the AQ prefix)
        {
            Console.WriteLine("Detected legacy hash format");
            var legacyHasher = new Microsoft.AspNet.Identity.PasswordHasher();
            var result = legacyHasher.VerifyHashedPassword(user.PasswordHash, password);
            Console.WriteLine($"Legacy verification result: {result}");

            var verificationResult = result == PasswordVerificationResult.Success ?
                PasswordVerificationResult.SuccessRehashNeeded : result;
            Console.WriteLine($"Legacy verification result: {verificationResult}");

            return result;
        }
    }

}
