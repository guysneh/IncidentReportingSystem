using MediatR;
using System;
using System.Collections.Generic;

namespace IncidentReportingSystem.Application.Features.Users.Queries.GetCurrentUserProfile
{
    /// <summary>
    /// Returns the currently authenticated user's profile, as stored in the database.
    /// </summary>
    public sealed record GetCurrentUserProfileQuery : IRequest<CurrentUserProfileDto>;

    /// <summary>Read-only DTO for the authenticated user's profile.</summary>
    public sealed record CurrentUserProfileDto(
        Guid Id,
        string Email,
        string? FirstName,
        string? LastName,
        IReadOnlyCollection<string> Roles,
        string DisplayName
    );
}
