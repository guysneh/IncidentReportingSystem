using MediatR;

namespace IncidentReportingSystem.Application.Features.Users.Commands.UpdateUserProfile
{
    /// <summary>
    /// Command to update the authenticated user's profile (first/last name).
    /// The target user is inferred from the current execution context (ICurrentUserService).
    /// </summary>
    /// <param name="FirstName">New first name (1–50, letters/spaces/'/- only).</param>
    /// <param name="LastName">New last name (1–50, letters/spaces/'/- only).</param>
    public sealed record UpdateUserProfileCommand(string FirstName, string LastName)
        : IRequest<UserProfileDto>;
}
