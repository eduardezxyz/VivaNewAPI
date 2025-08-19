using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Security.Claims;
using NewVivaApi.Models;
using NewVivaApi.Services;
using System.Text.Json;
using NewVivaApi.Authentication.Models;

namespace NewVivaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        // private readonly SignInManager<ApplicationUser> _signInManager;
        // private readonly IEmailSender _emailSender; // replace with your EmailService wrapper

        public AccountController(
            UserManager<ApplicationUser> userManager
            // SignInManager<ApplicationUser> signInManager
            // IEmailSender emailSender
            )
        {
            _userManager = userManager;
            // _signInManager = signInManager;
            // _emailSender = emailSender;
        }

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
                var requirements = new
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
            // catch (UserCreationException uce)
            // {
            //     Console.WriteLine($"User creation failed: {string.Join(", ", uce.IdentityResult.Errors.Select(e => e.Description))}");

            //     // Check for duplicate email error
            //     foreach (var error in uce.IdentityResult.Errors)
            //     {
            //         if (error.Code == "DuplicateEmail" || error.Description.Contains("already exists"))
            //         {
            //             return BadRequest(new { Type = "error", Message = "Email already exists" });
            //         }
            //     }

            //     return BadRequest(new
            //     {
            //         Type = "error",
            //         Message = "User registration failed",
            //         Errors = uce.IdentityResult.Errors.Select(e => e.Description)
            //     });
            // }
            catch (Exception ex)
            {
                Console.WriteLine($"Registration error: {ex.Message}");
                return StatusCode(500, new { Type = "error", Message = "Internal server error" });
            }
        }

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
    }
}
