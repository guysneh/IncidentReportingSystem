using System.Security.Claims;

namespace IncidentReportingSystem.Application.Abstractions.Identity;

public interface ICurrentUser
{
    string? UserId { get; }
    string? FirstName { get; }
    string? LastName { get; }
    string? Email { get; }
    ClaimsPrincipal? Principal { get; }
}
