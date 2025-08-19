using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using NewVivaApi.Authentication;
using NewVivaApi.Models;
using NewVivaApi.Services;
using Microsoft.AspNet.Identity; // Old Identity v2
using Microsoft.EntityFrameworkCore;
using NewVivaApi.Data; // Make sure this points to your AppDbContext namespace
using NewVivaApi.Authentication.Models;
// using Microsoft.AspNetCore.Identity; // Use this instead (new Identity)
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using NewVivaApi.Authentication.Models;
using Microsoft.AspNetCore.Identity;

namespace NewVivaApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly AuthService _service;
    private readonly IdentityDbContext _identityDbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;

    public AuthController(AppDbContext context, AuthService service, IdentityDbContext identityDbContext, Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = context;
        _service = service;
        _identityDbContext = identityDbContext;
        _dbContext = context;
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
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

        if (verificationResult == Microsoft.AspNet.Identity.PasswordVerificationResult.Success)
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

        // if (User.Identity.IsAuthenticated && IsServiceUser())
        // {
        //     return BadRequest();
        // }

        // // Use ModelState validation instead of manual validation
        // if (!ModelState.IsValid)
        // {
        //     return BadRequest(ModelState);
        // }

        try
        {
            Console.WriteLine("Extracting data from request...");
            // Extract data from JSON (avoiding model binding issues)
            var extractedData = ExtractRegisterData(data);

            Console.WriteLine($"Extracted data: {extractedData.FirstName} {extractedData.LastName}, Email: {extractedData.Email}, Phone: {extractedData.PhoneNumber}, CompanyID: {extractedData.CompanyID}, isAdminTF: {extractedData.IsAdminTF}, isGCTF: {extractedData.IsGCTF}, isSCTF: {extractedData.IsSCTF}, gcApproveTF: {extractedData.GcApproveTF}");
            // Manual validation
            var validationErrors = ValidateRegisterData(extractedData);
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
            Console.WriteLine($"Generated password for user: {extractedData.Email}");

            //Create model with proper constructor and dependencies
            var model = new RegisterSystemUserModel(_dbContext, _userManager, _httpContextAccessor)
            {
                UserName = extractedData.Email,
                FirstName = extractedData.FirstName,
                LastName = extractedData.LastName,
                PhoneNumber = extractedData.PhoneNumber,
                Password = generatedPassword,
                ConfirmPassword = generatedPassword,
                CompanyID = extractedData.CompanyID,
                isAdminTF = extractedData.IsAdminTF,
                isGCTF = extractedData.IsGCTF,
                isSCTF = extractedData.IsSCTF,
                gcApproveTF = extractedData.GcApproveTF
            };

            // Register the user
            var creatorUserName = User?.Identity?.Name ?? string.Empty;
            Console.WriteLine($"Registering user: {extractedData.Email} by {creatorUserName}");

            await model.RegisterAsync(creatorUserName);
            Console.WriteLine("User registration completed.");

            return Ok(new
            {
                Type = "success",
                Message = "User registered successfully!",
                UserId = model.GetUserId(),
                GeneratedPassword = generatedPassword // Remove in production
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
    private RegisterDataModel ExtractRegisterData(JsonElement data)
    {
        Console.WriteLine("Extracting register data from JSON...");
        var firstName = GetJsonProperty(data, "firstName");
        var lastName = GetJsonProperty(data, "lastName");
        var email = GetJsonProperty(data, "email");
        var phoneNumber = GetJsonProperty(data, "phoneNumber");
        Console.WriteLine($"Extracted: {firstName} {lastName}, Email: {email}, Phone: {phoneNumber}");

        int companyId = 0;
        if (data.TryGetProperty("companyID", out var companyIdElement))
        {
            if (companyIdElement.ValueKind == JsonValueKind.Number)
            {
                companyId = companyIdElement.GetInt32();
            }
            else if (companyIdElement.ValueKind == JsonValueKind.String)
            {
                int.TryParse(companyIdElement.GetString(), out companyId);
            }
        }
        // Handle boolean properties properly
        bool isAdminTF = GetJsonBoolean(data, "isAdminTF");
        Console.WriteLine($"Parsed isAdminTF: {isAdminTF}");
        bool isGCTF = GetJsonBoolean(data, "isGCTF");
        Console.WriteLine($"Parsed isGCTF: {isGCTF}");
        bool isSCTF = GetJsonBoolean(data, "isSCTF");
        Console.WriteLine($"Parsed isSCTF: {isSCTF}");
        bool gcApproveTF = GetJsonBoolean(data, "gcApproveTF");
        Console.WriteLine($"Parsed gcApproveTF: {gcApproveTF}");
        return new RegisterDataModel
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = phoneNumber,
            CompanyID = companyId,
            IsAdminTF = isAdminTF,
            IsGCTF = isGCTF,
            IsSCTF = isSCTF,
            GcApproveTF = gcApproveTF
        };
    }

    private string GetJsonProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) ? prop.GetString() ?? "" : "";
    }

    // New helper method to handle boolean properties correctly
    private bool GetJsonBoolean(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.True)
                return true;
            else if (prop.ValueKind == JsonValueKind.False)
                return false;
            else if (prop.ValueKind == JsonValueKind.String)
            {
                // Handle string representations of booleans
                bool.TryParse(prop.GetString(), out bool result);
                return result;
            }
        }
        return false; // Default to false if property doesn't exist or can't be parsed
    }

    //validation for new data model
    private List<string> ValidateRegisterData(RegisterDataModel data)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(data.FirstName))
            errors.Add("First name is required");

        if (string.IsNullOrWhiteSpace(data.LastName))
            errors.Add("Last name is required");

        if (string.IsNullOrWhiteSpace(data.Email))
            errors.Add("Email is required");
        else if (!IsValidEmail(data.Email))
            errors.Add("Invalid email format");

        // Validate that at least one role is selected
        if (!data.IsAdminTF && !data.IsGCTF && !data.IsSCTF)
            errors.Add("At least one role must be selected (Admin, General Contractor, or Subcontractor)");

        // CompanyID is required for GC and SC roles
        if ((data.IsGCTF || data.IsSCTF) && data.CompanyID <= 0)
            errors.Add("CompanyID is required when assigning General Contractor or Subcontractor roles");

        return errors;
    }

    // Helper method to check if user is a service user
    private bool IsServiceUser()
    {
        // Implement your service user check logic here
        // This could check claims, roles, or other identity properties
        return User.IsInRole("ServiceUser") || User.HasClaim("UserType", "Service");
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

    private Microsoft.AspNet.Identity.PasswordVerificationResult VerifyPasswordWithDetection(ApplicationUser user, string password)
    {
        // Detect hash format
        if (user.PasswordHash.StartsWith("AQAAAA")) // Modern ASP.NET Core format
        {
            Console.WriteLine("Detected modern hash format");
            var modernHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<ApplicationUser>();
            return (Microsoft.AspNet.Identity.PasswordVerificationResult)modernHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        }
        else // Legacy format (usually base64 without the AQ prefix)
        {
            Console.WriteLine("Detected legacy hash format");
            var legacyHasher = new Microsoft.AspNet.Identity.PasswordHasher();
            var result = legacyHasher.VerifyHashedPassword(user.PasswordHash, password);
            Console.WriteLine($"Legacy verification result: {result}");

            var verificationResult = result == Microsoft.AspNet.Identity.PasswordVerificationResult.Success ?
                Microsoft.AspNet.Identity.PasswordVerificationResult.SuccessRehashNeeded : result;
            Console.WriteLine($"Legacy verification result: {verificationResult}");

            return result;
        }
    }

}

public class RegisterDataModel
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public int CompanyID { get; set; }
    public bool IsAdminTF { get; set; }
    public bool IsGCTF { get; set; }
    public bool IsSCTF { get; set; }
    public bool GcApproveTF { get; set; }
}
