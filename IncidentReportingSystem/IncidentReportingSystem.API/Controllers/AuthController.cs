using Asp.Versioning;
using IncidentReportingSystem.API.Common;
using IncidentReportingSystem.API.Contracts.Authentication;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Application.Common.Exceptions;
using IncidentReportingSystem.Application.Common.Logging;
using IncidentReportingSystem.Application.Features.Users.Commands.ChangePassword;
using IncidentReportingSystem.Application.Features.Users.Commands.LoginUser;
using IncidentReportingSystem.Application.Features.Users.Commands.RegisterUser;
using IncidentReportingSystem.Application.Features.Users.Commands.UpdateUserProfile;
using IncidentReportingSystem.Application.Features.Users.Queries.GetCurrentUserProfile;
using IncidentReportingSystem.Domain;
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
    [Tags("Auth")]
    public sealed class AuthController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ISender sender, ICurrentUserService currentUser, ILogger<AuthController> logger)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _logger = logger;
        }

        /// <summary>Registers a new user with roles. Anonymous for demo.</summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest body, CancellationToken ct)
        {
            var roles = (body.Roles is { Length: > 0 })
                ? body.Roles!
                : string.IsNullOrWhiteSpace(body.Role)
                    ? new[] { Roles.User }
                    : new[] { body.Role! };

            var cmd = new RegisterUserCommand(
                Email: body.Email,
                Password: body.Password,
                Roles: roles,
                FirstName: body.FirstName,
                LastName: body.LastName
            );

            try
            {
                var created = await _sender.Send(cmd, ct);

                // issue token right after registration
                var login = await _sender.Send(new LoginUserCommand(body.Email, body.Password), ct);

                return CreatedAtAction(nameof(Me), new { }, new
                {
                    id = created.UserId,
                    accessToken = login.AccessToken,
                    expiresAtUtc = login.ExpiresAtUtc,
                    email = body.Email,
                    firstName = body.FirstName,
                    lastName = body.LastName,
                    displayName = string.Join(" ", new[] { body.FirstName, body.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)))
                });
            }
            catch (ConflictException)
            {
                return Conflict(new { error = "User already exists" });
            }
        }


        /// <summary>Authenticate with email + password and receive a JWT.</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest body, CancellationToken cancellationToken)
        {
            using var scope = _logger.BeginAuditScope(AuditTags.Auth, AuditTags.Login);
            var result = await _sender.Send(new LoginUserCommand(body.Email, body.Password), cancellationToken)
                                      .ConfigureAwait(false);
            // Emit audit log with structured tags. Do not include password or raw tokens.
            _logger.LogInformation(
               AuditEvents.Auth.Login,
               "Audit: {tags}",
               "auth,login");

            return Ok(new LoginResponse
            {
                AccessToken = result.AccessToken,
                ExpiresAtUtc = result.ExpiresAtUtc
            });
        }

        /// <summary>Update the authenticated user's first/last name.</summary>
        [HttpPatch("me")]
        [Authorize]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserProfileDto>> UpdateMe([FromBody] UpdateMeRequest body, CancellationToken ct)
        {
            var dto = await _sender.Send(new UpdateUserProfileCommand(body.FirstName, body.LastName), ct);
            return Ok(dto);
        }

        public sealed record UpdateMeRequest(string FirstName, string LastName);

        /// <summary>Return current authenticated user's profile (fresh from DB).</summary>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(WhoAmIResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<WhoAmIResponse>> Me(CancellationToken ct)
        {
            var profile = await _sender.Send(new GetCurrentUserProfileQuery(), ct);

            var resp = new WhoAmIResponse(
                profile.Id.ToString(),
                profile.Email,
                profile.Roles.ToArray(),
                profile.FirstName,
                profile.LastName,
                profile.DisplayName
            );

            return Ok(resp);
        }

        /// <summary>Change the authenticated user's password.</summary>
        /// <response code="204">Password changed.</response>
        /// <response code="400">Validation errors / weak password.</response>
        /// <response code="401">Missing/invalid token.</response>
        /// <response code="403">Current password does not match.</response>
        [HttpPost("me/change-password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest body, CancellationToken ct)
        {
            await _sender.Send(new ChangePasswordCommand(body.CurrentPassword, body.NewPassword), ct);
            return NoContent();
        }

        // If you must also support the users/me form, add an extra route:
        [HttpPost("~/api/v{version:apiVersion}/users/me/change-password")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public Task<IActionResult> ChangePasswordAlias([FromBody] ChangePasswordRequest body, CancellationToken ct)
            => ChangePassword(body, ct);
    }
}
