namespace IncidentReportingSystem.Application.Features.Users.Commands.UpdateUserProfile
{
    /// <summary>
    /// UI-ready projection of the user's profile after an update.
    /// </summary>
    public sealed record UserProfileDto(
        Guid Id,
        string Email,
        string FirstName,
        string LastName,
        string DisplayName,
        DateTime CreatedAtUtc,
        DateTime? ModifiedAtUtc
    );
}
