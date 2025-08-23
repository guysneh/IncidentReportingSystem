using MediatR;

namespace IncidentReportingSystem.Application.Features.Users.Commands.LoginUser
{
    /// <summary>
    /// Command to authenticate with email + password and receive a JWT.
    /// </summary>
    public sealed record LoginUserCommand(string Email, string Password) : IRequest<LoginResultDto>;

    /// <summary>
    /// Result model containing access token and expiry.
    /// </summary>
    public sealed record LoginResultDto(string AccessToken, DateTimeOffset ExpiresAtUtc);
}