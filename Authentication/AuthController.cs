using NewVivaApi.Authentication;
using NewVivaApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace NewVivaApi.Controllers;

[Route("auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private AuthService _service;
    private readonly IConfiguration _configuration;

    public AuthController(AuthService service, IConfiguration configuration)
    {
        _service = service;
        _configuration = configuration;
    }

    [HttpPost]
    [Route("Login")]
    public async Task<ActionResult> Login([FromBody] LoginModel model)
    {
        Console.WriteLine("=== AUTH LOGIN ===");
        Console.WriteLine($" (AuthController) Model: {model}");
        var result = await _service.Login(model);
        // if (result == null)
        // {
        //     return BadRequest(new Response { Type = "error", Message = "Login failed!" });
        // }
        // if (await _service.IsPasswordExpired(model.Username))
        // {
        //     var redirectLink = await _service.GetResetRedirectLink(model.Username);
        //     return StatusCode(302, new DataResponse<string>() { Type = "success", Message = "2FA Required!", Data = redirectLink});
        // }
        // if (await _service.IsTwoFactorEnabled(model.Username))
        // {
        //     if (!string.IsNullOrEmpty(model.Code))
        //     {
        //         if (await _service.Check2FACode(model.Username, model.Code))
        //         {
        //             return Ok(new DataResponse<string> { Type = "success", Message = "Login successful!", Data = result });
        //         }
        //         return Unauthorized(new Response { Type = "error", Message = "Invalid two-factor authentication code!" });
        //     }
        //     if (await _service.Send2FACode(model.Username))
        //     {
        //         return StatusCode(250, new Response { Type = "success", Message = "2FA Required!" });
        //     }
        //     return BadRequest(new Response { Type = "error", Message = "Error sending 2FA Code!" });
        // }
        return Ok(new DataResponse<string> { Type = "success", Message = "Login successful!" });
    }

    // [HttpPost]
    // [Route("Register/{token}")]
    // public async Task<ActionResult> Register([FromBody] PasswordDTO model, [FromRoute] string token, [FromQuery] string username)
    // {
    //     var result = await _service.Register(model, token, username);
    //     if (result == null)
    //         return StatusCode(500, new Response { Type = "error", Message = "User registering failed!" });
    //     return Ok(new DataResponse<UserDTO> { Type = "success", Message = "User registered successfully!", Data = result });
    // }

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
}

    public class SimpleLoginRequest
{
    public string username { get; set; } = "";  // lowercase to match JSON
    public string password { get; set; } = "";  // lowercase to match JSON
}