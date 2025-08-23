using Asp.Versioning;
using IncidentReportingSystem.API.Contracts.Authentication;
using IncidentReportingSystem.Application.Features.Users.Commands.LoginUser;
using IncidentReportingSystem.Application.Features.Users.Commands.RegisterUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security;

namespace IncidentReportingSystem.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public sealed class AuthController : ControllerBase
    {
        private readonly ISender _sender;

        public AuthController(ISender sender)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        }

        /// <summary>Registers a new user with roles. Anonymous for demo.</summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RegisterUserResult), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken ct)
        {
            var result = await _sender.Send(command, ct).ConfigureAwait(false);
            return CreatedAtAction(nameof(Register), new { id = result.UserId }, result);
        }

        /// <summary>Authenticate with email + password and receive a JWT.</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest body, CancellationToken ct)
        {
            var result = await _sender.Send(new LoginUserCommand(body.Email, body.Password), ct)
                                      .ConfigureAwait(false);

            return Ok(new LoginResponse
            {
                AccessToken = result.AccessToken,
                ExpiresAtUtc = result.ExpiresAtUtc
            });
        }
    }
}
