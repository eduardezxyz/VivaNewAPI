using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using NewVivaApi.Authentication;
using NewVivaApi.Authentication.Models;
// using backend.Services.Messaging;
// using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using NewVivaApi.Services;

namespace NewVivaApi.Authentication;
public class AuthService
{
    // private readonly UserManager<ApplicationUser> _userManager;
    // private readonly IConfiguration _configuration;
    // private readonly ILogger<AuthService> _logger;
    // private readonly IEmailService _service;
    // private readonly IHttpContextAccessor _contextAccessor;
    // private readonly TwilioService _smsService;
    // private readonly SignInManager<ApplicationUser> _signInManager;
    // private readonly AspNetUserService _aspNetUserService;

    public AuthService(
        // UserManager<ApplicationUser> userManager,
        // ILogger<AuthService> logger,
        // IConfiguration configuration,
        // IHttpContextAccessor contextAccessor,
        // IEmailService service,
        // TwilioService smsService,
        // SignInManager<ApplicationUser> signInManager,
        // AspNetUserService aspNetUserService
        )
    {
        // _userManager = userManager;
        // _configuration = configuration;
        // _contextAccessor = contextAccessor;
        // _service = service;
        // _logger = logger;
        // _smsService = smsService;
        // _signInManager = signInManager;
        // _aspNetUserService = aspNetUserService;
    }

    public async Task<string?> Login([FromBody] LoginModel model)
    {
        Console.WriteLine($"Model: {model}");
        Console.WriteLine($"(AuthService) Logging in user: {model.Username}");

        // var user = await _userManager.FindByNameAsync(model.Username);
        //var user = await _userManager.FindUserWithLogging(model.UserName);
        Console.WriteLine($"FindByNameAsync returned: {user != null}");

        
        if (user == null)
        {
            _logger.LogError("User not found.");
            return null;
        }
        Console.WriteLine($"Logging in user: {user.UserName}");

        // if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
        // {
            Console.WriteLine("Attempting to get token for user.");
        //     return await GetToken(user);
        // }
        // _logger.LogError("Error logging in.");
        return null;
    }

    // public async Task<bool> IsUserInactive(string email)
    // {
    //     var aspNetUser = await _aspNetUserService.FindUserByUserName(email);
    //     if ((aspNetUser != null) && (aspNetUser.DeleteDt != null))
    //     {
    //         _logger.LogError("Error logging in. User inactive.");
    //         return true;
    //     }
    //     return false;
    // }

    // public async Task<string?> LoginWithActiveDirectory(string token)
    // {
    //     var authority = $"{_configuration["ExternalAuthProviders:AzureAD:Authority"]}{_configuration["ExternalAuthProviders:AzureAD:TenantID"]}";
    //     var audience = _configuration["ExternalAuthProviders:AzureAD:ClientId"];
    //     var discoveryEndpoint = $"{authority}/v2.0/.well-known/openid-configuration";
    //     var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(discoveryEndpoint, new OpenIdConnectConfigurationRetriever());

    //     var config = await configManager.GetConfigurationAsync();
    //     var issuer = config.Issuer;
    //     var signingKeys = config.SigningKeys.ToList();

    //     var validationParams = new TokenValidationParameters()
    //     {
    //         ValidAudience = audience,
    //         ValidIssuer = issuer,
    //         IssuerSigningKeys = signingKeys,
    //     };

    //     var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
    //     try
    //     {
    //         var jwt = await jwtSecurityTokenHandler.ValidateTokenAsync(token, validationParams);
    //         var email = jwt.Claims.FirstOrDefault(c => c.Key == "preferred_username").Value;
    //         if (email == null)
    //         {
    //             _logger.LogError("Username claim not found.");
    //             return null;
    //         }
    //         var user = await _userManager.FindByNameAsync((string)email);
    //         if (user == null)
    //         {
    //             _logger.LogError("User not found.");
    //             return null;
    //         }
    //         return await GetToken(user);
    //     }
    //     catch
    //     {
    //         _logger.LogError("Error authenticating Active Directory login.");
    //         return null;
    //     }
    // }

    // public async Task<User?> Register([FromBody] PasswordDTO model, string token, string username)
    // {
    //     var decodedUsername = Uri.UnescapeDataString(username);
    //     var user = await _userManager.FindByNameAsync(decodedUsername);
    //     if (user == null)
    //     {
    //         _logger.LogError("Error finding user.");
    //         return null;
    //     }
    //     var decodedToken = Uri.UnescapeDataString(token);
    //     var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
    //     if (result.Succeeded)
    //     {
    //         user.SecurityStamp = Guid.NewGuid().ToString();
    //         var newResult = await _userManager.AddPasswordAsync(user, model.NewPassword);
    //         if (!newResult.Succeeded)
    //         {
    //             _logger.LogError("Error registering user.");
    //             return null;
    //         }
    //         user.LastPasswordReset = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
    //         var updateResult = await _userManager.UpdateAsync(user);
    //         if (!updateResult.Succeeded)
    //         {
    //             _logger.LogError("Error updating password reset date time on user.");
    //             return null;
    //         }
    //         var roles = await _userManager.GetRolesAsync(user);
    //         var retUser = new User(user);
    //         retUser.Roles = roles.Cast<string>().ToArray();
    //         return retUser;
    //     }
    //     _logger.LogError("Error confirming email token.");
    //     return null;
    // }

    // public async Task<string?> SignInWithGoogle(string token)
    // {
    //     Boolean.TryParse(_configuration["AllowExternalLogin"], out var allowExternalLogin);
    //     if (!allowExternalLogin)
    //     {
    //         _logger.LogError("External login not allowed");
    //         return null;
    //     }
    //     var info = await _signInManager.GetExternalLoginInfoAsync();

    //     var validationSettings = new GoogleJsonWebSignature.ValidationSettings
    //     {
    //         Audience = new string[] { _configuration["ExternalAuthProviders:Google:ClientId"]! }
    //     };
    //     try
    //     {
    //         var payload = await GoogleJsonWebSignature.ValidateAsync(token, validationSettings);
    //         var user = await _userManager.FindByNameAsync(payload.Email);
    //         if (user == null)
    //         {
    //             _logger.LogError("User does not exist.");
    //             return null;
    //         }
    //         return await GetToken(user, token);
    //     }
    //     catch (Exception e)
    //     {
    //         _logger.LogError("Error authenticating Google login");
    //         return null;
    //     }
    // }

    // public async Task<string> TokenRefresh()
    // {
    //     var contextUser = _contextAccessor.HttpContext!.User;
    //     var user = await _userManager.FindByNameAsync(contextUser.Identity!.Name);
    //     var token = contextUser.Claims.FirstOrDefault(t => t.Type == "ExternalToken")?.Value ?? null;
    //     return await GetToken(user, token);
    // }

    // private async Task<string> GetToken(ApplicationUser user, string? externalToken = "")
    // {

    //     var authClaims = new List<Claim>
    //     {
    //         new Claim(ClaimTypes.Name, user.UserName),
    //         new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    //         new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString())
    //     };

        // var userRoles = (await _userManager.GetRolesAsync(user)).ToList();

        // userRoles.ForEach(r => authClaims.Add(new Claim(ClaimTypes.Role, r)));

        // if (!string.IsNullOrEmpty(externalToken))
        // {
        //     authClaims.Add(new Claim("ExternalToken", externalToken));
        // }

    //     var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

    //     var token = new JwtSecurityToken(
    //         issuer: _configuration["JWT:ValidIssuer"],
    //         audience: _configuration["JWT:ValidAudience"],
    //         expires: DateTime.Now.AddHours(8),
    //         claims: authClaims,
    //         signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
    //     );

    //     return new JwtSecurityTokenHandler().WriteToken(token);
    // }

    // public async Task<bool> IsPasswordExpired(string username)
    // {
    //     var passwordExpirationString = _configuration["PasswordExpiration"];
    //     if (string.IsNullOrEmpty(passwordExpirationString))
    //     {
    //         return false;
    //     }
    //     var passwordExpirationTime = int.Parse(passwordExpirationString);
    //     var expirationDate = DateTime.SpecifyKind(DateTime.Now.AddDays(-passwordExpirationTime), DateTimeKind.Utc);
    //     var user = await _userManager.FindByNameAsync(username);
    //     if (user.LastPasswordReset < expirationDate)
    //     {
    //         _logger.LogInformation($"Password for user {username} is expired.");
    //         return true;
    //     }
    //     return false;
    // }

    // public async Task<bool> IsTwoFactorEnabled(string username)
    // {
    //     var user = await _userManager.FindByNameAsync(username);
    //     return user.TwoFactorEnabled;
    // }

    // public async Task<string> GetResetRedirectLink(string username)
    // {
    //     var user = await _userManager.FindByNameAsync(username);
    //     var token = await _userManager.GeneratePasswordResetTokenAsync(user);
    //     string encodedToken = Uri.EscapeDataString(token);
    //     return "reset-expired-password/" + encodedToken + "?username=" + Uri.EscapeDataString(username);
    // }

    // public async Task<bool> Check2FACode(string username, string code)
    // {
    //     var user = await _userManager.FindByNameAsync(username);
    //     var check = await _userManager.VerifyTwoFactorTokenAsync(user, "Email", code);
    //     return check;
    // }

    // public async Task<bool> SetPasswordForUser(string username, string password)
    // {
    //     var user = await _userManager.FindByNameAsync(username);
    //     if (user == null)
    //     {
    //         return false;
    //     }
    //     user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, password);
    //     user.LastPasswordReset = DateTime.Now;
    //     await _userManager.UpdateAsync(user);
    //     return true;
    // }

    // public async Task<bool> Send2FACode(string username, string? method = null)
    // {
    //     bool result = false;
    //     var user = await _userManager.FindByNameAsync(username);
    //     var code = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
    //     method ??= user.TwoFactorType;
    //     switch (method)
    //     {
    //         case "text":
    //             result = await _smsService.SendSms(user.PhoneNumber, "Your authentication code is " + code);
    //             break;
    //         case "email":
    //         default:
    //             result = await _service.SendLogin2FAEmail(code, username);
    //             break;
    //     }
    //     return result;
    // }

    // public async Task<string?> Impersonate(string id)
    // {

    //     var currentUserId = _contextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    //     if (currentUserId == null)
    //     {
    //         _logger.LogWarning("Current user ID could not be retrieved.");
    //         return null;
    //     }

    //     if (currentUserId == id)
    //     {
    //         _logger.LogWarning("Attempt to impersonate oneself detected.");
    //         return null;
    //     }

    //     var user = await _userManager.FindByIdAsync(id);
    //     if (user != null)
    //     {
    //         return await GetToken(user);
    //     }
    //     _logger.LogError("Error finding user.");
    //     return null;
    // }
}
