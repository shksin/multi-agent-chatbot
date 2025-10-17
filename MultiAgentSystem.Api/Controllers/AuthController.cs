using Microsoft.AspNetCore.Mvc;
using MultiAgentSystem.Api.Services;

namespace MultiAgentSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { message = "Username and password are required" });
        }

        var response = await _authService.AuthenticateAsync(request.Username, request.Password);
        
        if (response == null)
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        _logger.LogInformation("User {Username} logged in successfully", request.Username);
        return Ok(response);
    }

    [HttpPost("validate")]
    public IActionResult ValidateToken([FromBody] ValidateTokenRequest request)
    {
        if (string.IsNullOrEmpty(request.Token))
        {
            return BadRequest(new { message = "Token is required" });
        }

        var isValid = _authService.ValidateToken(request.Token);
        return Ok(new { isValid });
    }

    [HttpGet("demo-users")]
    public IActionResult GetDemoUsers()
    {
        return Ok(new
        {
            users = new[]
            {
                new { username = "demo", password = "demo123", description = "Demo account" }
            }
        });
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ValidateTokenRequest
{
    public string Token { get; set; } = string.Empty;
}
