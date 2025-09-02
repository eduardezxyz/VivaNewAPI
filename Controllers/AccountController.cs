using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;
using NewVivaApi.Authentication.Models; // ApplicationUser, Role
using NewVivaApi.Data;                  // AppDbContext
using NewVivaApi.Authentication.Models; // IdentityDbContext (your identity-only DbContext)
using NewVivaApi.Models;                // UserProfile, AspNetUserExtension, etc.
// using ... your services/namespaces:
using NewVivaApi.Services;
using NewVivaApi.Models;
//using NewVivaApi.Models.Exceptions;
using NewVivaApi.Authentication;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.JwtBearer;
// using VivaPayAppAPI.Providers; // if you still have helper classes
// using VivaPayAppAPI.Results;
using NewVivaApi.Services;
using NewVivaApi.Extensions;
using Microsoft.AspNet.Identity;

namespace VivaPayAppAPI.Controllers;

[ApiController]
//[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private const string LocalLoginProvider = "Local";

    private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IAuthenticationSchemeProvider _schemeProvider;
    private readonly IdentityDbContext _identityDb;
    private readonly AppDbContext _appDbcontext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly EmailService _emailService;
    //private readonly PasswordGenerationService _pwdService;   // your existing service

    public AccountController(
        Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAuthenticationSchemeProvider schemeProvider,
        IdentityDbContext identityDb,
        IHttpContextAccessor httpContextAccessor,
        EmailService emailService,
        AppDbContext appDb)
    //PasswordGenerationService pwdService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _schemeProvider = schemeProvider;
        _identityDb = identityDb;
        _appDbcontext = appDb;
        _httpContextAccessor = httpContextAccessor;
        _emailService = emailService;
        //_pwdService    = pwdService;
    }


    // GET api/Account/UserInfo
    [HttpGet("UserInfo")]
    [AllowAnonymous] // keep same behavior you had (Bearer-only host auth is OWIN-specific)
    public async Task<ActionResult<UserInfoViewModel>> GetUserInfo()
    {
        // If you still have this extension, keep it:
        if (User.Identity?.IsServiceUser() == true)
            return new UserInfoViewModel { };

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return new UserInfoViewModel { HasRegistered = false };
        }

        // In Core, external login provider info can be fetched from claims:
        var external = ExternalLoginData.FromIdentity(User.Identity as ClaimsIdentity);

        return new UserInfoViewModel
        {
            Email = user.UserName,
            HasRegistered = external == null,
            LoginProvider = external?.LoginProvider
        };
    }

    /*
        // POST api/Account/Logout
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            if (User.Identity?.IsServiceUser() == true) return BadRequest();

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok();
        }

        // GET api/Account/ManageInfo?returnUrl=%2F&generateState=true
        [HttpGet("ManageInfo")]
        public async Task<ActionResult<ManageInfoViewModel>> GetManageInfo([FromQuery] string returnUrl, [FromQuery] bool generateState = false)
        {
            if (User.Identity?.IsServiceUser() == true) return Ok(null);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Ok(null);

            var userLogins = await _userManager.GetLoginsAsync(user);
            var logins = userLogins.Select(l => new UserLoginInfoViewModel
            {
                LoginProvider = l.LoginProvider,
                ProviderKey = l.ProviderKey
            }).ToList();

            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (hasPassword)
            {
                logins.Add(new UserLoginInfoViewModel
                {
                    LoginProvider = LocalLoginProvider,
                    ProviderKey = user.UserName
                });
            }

            var externalLogins = await GetExternalLoginsInternal(returnUrl, generateState);

            return new ManageInfoViewModel
            {
                LocalLoginProvider = LocalLoginProvider,
                Email = user.UserName,
                Logins = logins,
                ExternalLoginProviders = externalLogins
            };
        }
*/
        // GET api/Account/SendPasswordLink?Email=...&domain=...
        [HttpGet("SendPasswordLink")]
        [AllowAnonymous]
        public async Task<IActionResult> SendPasswordLink([FromQuery] string Email, [FromQuery] string domain)
        {
            if (User.Identity?.IsServiceUser() == true) return BadRequest();
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var user = await _userManager.FindByEmailAsync(Email);
            if (user != null)
            {
                // Extension row in Identity DB
                var extension = await _identityDb.AspNetUserExtensions.FindAsync(user.Id);
                if (extension == null)
                {
                    extension = new AspNetUserExtension { Id = user.Id };
                    _identityDb.AspNetUserExtensions.Add(extension);
                }

                // Profile in App DB
                var profile = await _appDbcontext.UserProfiles.FindAsync(user.Id);

                // Generate token using your existing generator to keep backward compat
                var token = await TokenGenerator.GetToken(user.Id, _userManager);
                extension.PasswordResetIdentity = token.Identity;
                extension.PasswordResetToken = token.Value;
                extension.PasswordResetTokenExpiration = token.Expiration;

                await _identityDb.SaveChangesAsync();

                var firstName = profile?.FirstName ?? user.FirstName ?? "";
                _emailService.sendPasswordResetLinkEmail(Email, firstName, user.UserName, extension.PasswordResetIdentity!, domain);
            }

            return Ok();
        }

        // POST api/Account/ResetFromToken
        [HttpPost("ResetFromToken")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetFromToken([FromBody] ResetFromTokenData data)
        {
            if (User.Identity?.IsServiceUser() == true) return BadRequest();
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            if (!string.Equals(data.NewPassword, data.ConfirmPassword)) return BadRequest("Passwords do not match");

            string? userId = null;
            string? resetToken = null;
            string? firstName = null;
            string? email = null;

            // Look up extension by identity (and not expired)
            var extension = _identityDb.AspNetUserExtensions
                .FirstOrDefault(e => e.PasswordResetIdentity == data.Token &&
                                     e.PasswordResetTokenExpiration != null &&
                                     e.PasswordResetTokenExpiration > DateTime.UtcNow);

            if (extension != null)
            {
                userId = extension.Id;

                // gather email + name from App DB (profile)
                var profile = await _appDbcontext.UserProfiles.FindAsync(userId);
                if (profile != null)
                {
                    firstName = profile.FirstName;
                    email = profile.UserName;
                }

                // Back-compat: your token unwrapping
                resetToken = TokenGenerator.ExtractToken(new Token
                {
                    Identity = extension.PasswordResetIdentity!,
                    Value = extension.PasswordResetToken!
                });

                // clear token
                extension.PasswordResetIdentity = null;
                extension.PasswordResetTokenExpiration = null;
                extension.PasswordResetToken = null;
                await _identityDb.SaveChangesAsync();
            }
            
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return BadRequest("User not found");
            }

            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(resetToken))
        {
            var result = await _userManager.ResetPasswordAsync(user, resetToken, data.NewPassword);
            // if (!result.Succeeded)
            //     return FromIdentityError(result);

            _ = Task.Run(() => _emailService.sendPasswordChangedEmail(email ?? "", firstName ?? ""));

            return Ok();
        }

            return NotFound();
        }

    // POST api/Account/ChangePassword
    [HttpPost("ChangePassword")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordBindingModel model)
    {
        if (User.Identity?.IsServiceUser() == true)
        {
            return BadRequest();
        }
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        } 

        var user = await _userManager.FindByIdAsync(model.UserID);
        if (user == null)
        {
            return BadRequest("User not found");
        }

        var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

        if (!result.Succeeded)
        {
            //return FromIdentityError(result);
            return BadRequest("result not found");
        } 

        Console.WriteLine($"Result: {result}");

        await ClearPasswordResetFlag(model.UserID);
        return Ok();
    }

    /*
                    // POST api/Account/SetPassword
                    [HttpPost("SetPassword")]
                    public async Task<IActionResult> SetPassword([FromBody] SetPasswordBindingModel model)
                    {
                        if (User.Identity?.IsServiceUser() == true) return BadRequest();
                        if (!ModelState.IsValid) return ValidationProblem(ModelState);

                        var user = await _userManager.GetUserAsync(User);
                        if (user == null) return Unauthorized();

                        var result = await _userManager.AddPasswordAsync(user, model.NewPassword);
                        if (!result.Succeeded) return FromIdentityError(result);

                        return Ok();
                    }

                    // POST api/Account/AddExternalLogin
                    [HttpPost("AddExternalLogin")]
                    public async Task<IActionResult> AddExternalLogin([FromBody] AddExternalLoginBindingModel model)
                    {
                        if (User.Identity?.IsServiceUser() == true) return BadRequest();
                        if (!ModelState.IsValid) return ValidationProblem(ModelState);

                        // In Core, exchange external access token step differs; if you still carry the same format, keep your unprotect logic
                        var ticket = HttpContext.AuthenticateAsync(model.ExternalAccessToken).Result; // TODO: adapt if you store tokens differently
                        if (ticket == null || !ticket.Succeeded || (ticket.Properties?.ExpiresUtc <= DateTimeOffset.UtcNow))
                            return BadRequest("External login failure.");

                        var externalData = ExternalLoginData.FromIdentity(ticket.Principal.Identity as ClaimsIdentity);
                        if (externalData == null) return BadRequest("The external login is already associated with an account.");

                        var user = await _userManager.GetUserAsync(User);
                        if (user == null) return Unauthorized();

                        var result = await _userManager.AddLoginAsync(user, new UserLoginInfo(externalData.LoginProvider, externalData.ProviderKey, externalData.LoginProvider));
                        if (!result.Succeeded) return FromIdentityError(result);

                        return Ok();
                    }

                    // POST api/Account/RemoveLogin
                    [HttpPost("RemoveLogin")]
                    public async Task<IActionResult> RemoveLogin([FromBody] RemoveLoginBindingModel model)
                    {
                        if (User.Identity?.IsServiceUser() == true) return BadRequest();
                        if (!ModelState.IsValid) return ValidationProblem(ModelState);

                        var user = await _userManager.GetUserAsync(User);
                        if (user == null) return Unauthorized();

                        IdentityResult result;
                        if (model.LoginProvider == LocalLoginProvider)
                        {
                            result = await _userManager.RemovePasswordAsync(user);
                        }
                        else
                        {
                            result = await _userManager.RemoveLoginAsync(user, model.LoginProvider, model.ProviderKey);
                        }

                        if (!result.Succeeded) return FromIdentityError(result);
                        return Ok();
                    }

                    // GET api/Account/ExternalLogin?provider=...
                    [HttpGet("ExternalLogin")]
                    [AllowAnonymous]
                    public IActionResult ExternalLogin([FromQuery] string provider, [FromQuery] string? returnUrl = null, [FromQuery] string? error = null)
                    {
                        if (User.Identity?.IsServiceUser() == true) return BadRequest();
                        if (!string.IsNullOrEmpty(error)) return Redirect($"~/#error={Uri.EscapeDataString(error)}");

                        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
                        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
                        return Challenge(properties, provider);
                    }

                    // GET api/Account/ExternalLoginCallback
                    [HttpGet("ExternalLoginCallback")]
                    [AllowAnonymous]
                    public async Task<IActionResult> ExternalLoginCallback([FromQuery] string? returnUrl = null)
                    {
                        if (User.Identity?.IsServiceUser() == true) return BadRequest();

                        var info = await _signInManager.GetExternalLoginInfoAsync();
                        if (info == null) return Problem("External login info not found.");

                        var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
                        if (signInResult.Succeeded)
                        {
                            return Ok();
                        }

                        // Not registered yet — create identity from external claims so UI can finish registration if needed
                        var claims = info.Principal.Claims;
                        var id = new ClaimsIdentity(claims, OAuthDefaults.DisplayName);
                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
                        return Ok();
                    }

                    // GET api/Account/ExternalLogins?returnUrl=%2F&generateState=true
                    [HttpGet("ExternalLogins")]
                    [AllowAnonymous]
                    public async Task<IEnumerable<ExternalLoginViewModel>> ExternalLogins([FromQuery] string returnUrl, [FromQuery] bool generateState = false)
                    {
                        if (User.Identity?.IsServiceUser() == true) return Enumerable.Empty<ExternalLoginViewModel>();
                        return await GetExternalLoginsInternal(returnUrl, generateState);
                    }
                    */
    // POST api/Account/Register
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
            var model = new RegisterSystemUserModel(_appDbcontext, _userManager, _emailService, _httpContextAccessor)
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
        var firstName = GetJsonProperty(data, "FirstName");
        var lastName = GetJsonProperty(data, "LastName");
        var email = GetJsonProperty(data, "UserName");
        var phoneNumber = GetJsonProperty(data, "PhoneNumber");
        Console.WriteLine($"Extracted: {firstName} {lastName}, Email: {email}, Phone: {phoneNumber}");

        Console.WriteLine($"First name: {firstName}");
        Console.WriteLine($"Last name: {lastName}");
        Console.WriteLine($"Email: {email}");

        Console.WriteLine($"Raw JSON data: {data}");
        Console.WriteLine("Available properties:");
        foreach (var property in data.EnumerateObject())
        {
            Console.WriteLine($"  Property: '{property.Name}' = {property.Value} (Type: {property.Value.ValueKind})");
        }

        int companyId = 0;
        if (data.TryGetProperty("CompanyID", out var companyIdElement))
        {
            if (companyIdElement.ValueKind == JsonValueKind.Number)
            {
                companyId = companyIdElement.GetInt32();
                Console.WriteLine($"companyId: {companyId}");
            }
            else if (companyIdElement.ValueKind == JsonValueKind.String)
            {
                int.TryParse(companyIdElement.GetString(), out companyId);
                Console.WriteLine($"(string) companyId: {companyId}");
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

    //validation for new data model
    private List<string> ValidateRegisterData(RegisterDataModel data)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(data.FirstName))
        {
            errors.Add("First name is required");
        }

        if (string.IsNullOrWhiteSpace(data.LastName))
        {
            errors.Add("Last name is required");
        }

        if (string.IsNullOrWhiteSpace(data.Email))
        {
            errors.Add("Email is required");
        }

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

    /*
    // POST api/Account/RegisterExternal
    [HttpPost("RegisterExternal")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterExternal([FromBody] RegisterExternalBindingModel model)
    {
        if (User.Identity?.IsServiceUser() == true) return BadRequest();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null) return Problem("External login info not found.");

        var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded) return FromIdentityError(result);

        result = await _userManager.AddLoginAsync(user, info);
        if (!result.Succeeded) return FromIdentityError(result);

        return Ok();
    }

    // ---------- Helpers ----------

    private IActionResult FromIdentityError(IdentityResult result)
    {
        if (result == null) return Problem("Unknown identity error.");
        if (result.Succeeded) return Ok();

        foreach (var err in result.Errors)
            ModelState.AddModelError(string.Empty, err);

        return ValidationProblem(ModelState);
    }
*/
    private async Task ClearPasswordResetFlag(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            user.ResetPasswordOnLoginTF = false; // property exists on your ApplicationUser
            await _userManager.UpdateAsync(user);
        }
    }
    /*

    private async Task<IEnumerable<ExternalLoginViewModel>> GetExternalLoginsInternal(string returnUrl, bool generateState)
    {
        var schemes = await _schemeProvider.GetAllSchemesAsync();
        var externalSchemes = schemes.Where(s => !string.IsNullOrEmpty(s.DisplayName));

        string? state = generateState ? RandomOAuthStateGenerator.Generate(256) : null;

        var logins = new List<ExternalLoginViewModel>();
        foreach (var scheme in externalSchemes)
        {
            var url = Url.Action(nameof(ExternalLogin), new
            {
                provider = scheme.Name,
                response_type = "token",
                // client_id, redirect_uri depend on your SPA/native app – keep if needed:
                // client_id = Startup.PublicClientId,
                redirect_uri = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl }, Request.Scheme),
                state
            });

            logins.Add(new ExternalLoginViewModel
            {
                Name = scheme.DisplayName!,
                Url = url!,
                State = state
            });
        }
        return logins;
    }
 */
    // ---- Types ported from your code ----

    private class ExternalLoginData
    {
        public string LoginProvider { get; set; } = default!;
        public string ProviderKey { get; set; } = default!;
        public string? UserName { get; set; }

        public static ExternalLoginData? FromIdentity(ClaimsIdentity? identity)
        {
            if (identity == null) return null;
            var providerKeyClaim = identity.FindFirst(ClaimTypes.NameIdentifier);
            if (providerKeyClaim == null || string.IsNullOrEmpty(providerKeyClaim.Issuer) || string.IsNullOrEmpty(providerKeyClaim.Value))
                return null;
            if (providerKeyClaim.Issuer == ClaimsIdentity.DefaultIssuer)
                return null;

            return new ExternalLoginData
            {
                LoginProvider = providerKeyClaim.Issuer,
                ProviderKey = providerKeyClaim.Value,
                UserName = identity.FindFirstValue(ClaimTypes.Name)
            };
        }
    }

    private static class RandomOAuthStateGenerator
    {
        private static readonly RandomNumberGenerator _random = RandomNumberGenerator.Create();

        public static string Generate(int strengthInBits)
        {
            const int bitsPerByte = 8;
            if (strengthInBits % bitsPerByte != 0)
                throw new ArgumentException("strengthInBits must be evenly divisible by 8.", nameof(strengthInBits));

            int strengthInBytes = strengthInBits / bitsPerByte;
            var buffer = new byte[strengthInBytes];
            _random.GetBytes(buffer);
            // URL safe Base64 in Core:
            return Convert.ToBase64String(buffer)
                          .Replace('+', '-')
                          .Replace('/', '_')
                          .TrimEnd('=');
        }
    }
}
