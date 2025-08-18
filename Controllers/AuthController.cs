using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using NewVivaApi.Authentication;
using NewVivaApi.Models;
using NewVivaApi.Services;
using Microsoft.AspNet.Identity; // Old Identity v2
using Microsoft.EntityFrameworkCore;

namespace NewVivaApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    public AuthController(AuthService _service)
    {
        _authService = _service;
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var result = await _authService.Login(model);

        if (result == null)
            return Unauthorized(new { message = "Invalid username or password." });

        return Ok(result);
    }

}
