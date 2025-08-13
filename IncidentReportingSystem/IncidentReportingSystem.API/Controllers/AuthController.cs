using IncidentReportingSystem.API.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace IncidentReportingSystem.API.Controllers;

/// <summary>
/// Controller for generating JWT tokens for demo purposes.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IOptions<JwtSettings> _jwtOptions;
    public AuthController(IOptions<JwtSettings> jwtOptions)
    {
        _jwtOptions = jwtOptions;
    }

    /// <summary>
    /// Generates a demo JWT token for testing authenticated endpoints.
    /// </summary>
    /// <param name="userId">User ID to embed in token</param>
    /// <param name="role">User role (e.g., Admin, User)</param>
    /// <returns>JWT token as plain string</returns>
    [HttpGet("token")]
    public ActionResult<string> GetToken([FromQuery] string userId = "demo", [FromQuery] string role = "Admin")
    {
        var token = JwtTokenGenerator.GenerateToken(_jwtOptions, userId, role);
        return Ok(token);
    }
}
