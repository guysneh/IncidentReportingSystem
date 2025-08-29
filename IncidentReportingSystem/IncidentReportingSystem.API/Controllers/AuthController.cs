using Asp.Versioning;
using IncidentReportingSystem.API.Common;
using IncidentReportingSystem.API.Contracts.Authentication;
using IncidentReportingSystem.Application.Abstractions.Security;
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
        private readonly ICurrentUserService _currentUser;

        public AuthController(ISender sender, ICurrentUserService currentUser)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        }

        /// <summary>Registers a new user with roles. Anonymous for demo.</summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RegisterUserResult), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(command, cancellationToken).ConfigureAwait(false);
            return CreatedAtAction(nameof(Register), new { id = result.UserId }, result);
        }

        /// <summary>Authenticate with email + password and receive a JWT.</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest body, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new LoginUserCommand(body.Email, body.Password), cancellationToken)
                                      .ConfigureAwait(false);

            return Ok(new LoginResponse
            {
                AccessToken = result.AccessToken,
                ExpiresAtUtc = result.ExpiresAtUtc
            });
        }

        /// <summary>Returns information about the current authenticated user.</summary>
        /// <response code="200">User info returned.</response>
        /// <response code="401">Authentication required or invalid token.</response>
        [HttpGet("me")]
        [ProducesResponseType(typeof(WhoAmIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult<WhoAmIResponse> Me()
        {
            try
            {
                var userId = _currentUser.UserIdOrThrow();
                var email = HttpContext.User.GetEmail();
                var roles = HttpContext.User.GetRoles();

                // Enriched v2: OIDC-style name claims (if present)
                var firstName = HttpContext.User.FindFirst("given_name")?.Value;
                var lastName = HttpContext.User.FindFirst("family_name")?.Value;
                var display = HttpContext.User.FindFirst("name")?.Value ?? email;

                var dto = new WhoAmIResponse(
                    userId.ToString(),
                    email,
                    roles,
                    firstName,
                    lastName,
                    display);

                return Ok(dto);
            }
            catch (InvalidOperationException)
            {
                return Unauthorized();
            }
        }
    }
}
