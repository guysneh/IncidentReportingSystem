using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace IncidentReportingSystem.API.Controllers
{
    /// <summary>
    /// This controller provides a basic authentication endpoint for demonstration purposes only.
    /// It issues a JWT token when a valid username is provided.
    /// 
    /// WARNING:
    /// - This implementation uses a hardcoded username "admin" for simplicity.
    /// - No password or secure user validation is implemented here.
    /// - The secret key is read from configuration and must be at least 16 characters long.
    /// - This approach is NOT suitable for production environments.
    /// 
    /// For production, use a proper identity provider or authentication framework.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Login endpoint to authenticate user and generate JWT token.
        /// </summary>
        /// <param name="request">LoginRequest object containing the username</param>
        /// <returns>JWT token if username is valid; Unauthorized otherwise</returns>
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // For demo only: accept only username "admin"
            if (request.Username != "admin")
                return Unauthorized();

            // Define claims to include in the token
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, request.Username),
                new Claim(ClaimTypes.Role, "Admin")
            };

            // Read the secret key from configuration (must be long enough!)
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Create the token with expiration time of 1 hour
            var token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds
            );

            // Return the generated token as JSON
            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        }
    }

    /// <summary>
    /// Request DTO for login endpoint
    /// </summary>
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
    }
}
