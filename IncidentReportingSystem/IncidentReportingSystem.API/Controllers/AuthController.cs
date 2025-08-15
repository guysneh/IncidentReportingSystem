using IncidentReportingSystem.Application.Users.Commands.RegisterUser;
using IncidentReportingSystem.API.Auth; 
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace IncidentReportingSystem.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public sealed class AuthController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly IOptions<JwtSettings> _jwtOptions;

        public AuthController(ISender sender, IOptions<JwtSettings> jwtOptions)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
            _jwtOptions = jwtOptions ?? throw new ArgumentNullException(nameof(jwtOptions));
        }

        /// <summary>Registers a new user with roles. Anonymous for demo.</summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RegisterUserResult), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
        {
            if (command is null) return BadRequest("Invalid payload.");

            var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);
            return CreatedAtAction(nameof(Register), new { id = result.UserId }, result);
        }

        /// <summary>
        /// Generates a demo JWT token for testing authenticated endpoints.
        /// </summary>
        /// <param name="userId">User ID to embed in token</param>
        /// <param name="role">User role (e.g., Admin, User)</param>
        /// <returns>JWT token as plain string</returns>
        [HttpGet("token")]
        [AllowAnonymous]
        public ActionResult<string> GetToken([FromQuery] string userId = "demo", [FromQuery] string role = "Admin")
        {
            var token = JwtTokenGenerator.GenerateToken(_jwtOptions, userId, new[] { role });
            return Ok(token);
        }
    }
}
