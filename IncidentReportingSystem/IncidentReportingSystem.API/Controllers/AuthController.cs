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
            if (command is null) return BadRequest(new ProblemDetails { Title = "Invalid payload" });

            try
            {
                var result = await _sender.Send(command, ct).ConfigureAwait(false);
                return CreatedAtAction(nameof(Register), new { id = result.UserId }, result);
            }
            // Typical validation problems (FluentValidation or argument guards)
            catch (Exception ex) when (ex.GetType().Name == "ValidationException"
                                    || ex is ArgumentException
                                    || ex is ArgumentOutOfRangeException)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            // Duplicate email / uniqueness violations (use your own app exception if you have one)
            catch (Exception ex) when (ex.GetType().Name.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)
                                    || ex.GetType().Name.Contains("AlreadyExists", StringComparison.OrdinalIgnoreCase)
                                    || ex is DbUpdateException)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Email already exists",
                    Detail = ex.Message,
                    Status = StatusCodes.Status409Conflict
                });
            }
        }

        /// <summary>Authenticate with email + password and receive a JWT.</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest body, CancellationToken ct)
        {
            if (body is null) return BadRequest(new ProblemDetails { Title = "Invalid payload" });
            if (string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password))
                return BadRequest(new ProblemDetails { Title = "Email and password are required" });

            try
            {
                var result = await _sender.Send(new LoginUserCommand(body.Email, body.Password), ct)
                                          .ConfigureAwait(false);

                return Ok(new LoginResponse
                {
                    AccessToken = result.AccessToken,
                    ExpiresAtUtc = result.ExpiresAtUtc
                });
            }
            // Wrong email/password (use your specific app exception if available)
            catch (Exception ex) when (ex.GetType().Name == "InvalidCredentialsException"
                                     || ex is UnauthorizedAccessException
                                     || ex is SecurityException)
            {
                return Unauthorized(new ProblemDetails { Title = "Invalid credentials" });
            }
            // Input validation problems
            catch (Exception ex) when (ex.GetType().Name == "ValidationException"
                                     || ex is ArgumentException)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation failed",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }
    }
}
