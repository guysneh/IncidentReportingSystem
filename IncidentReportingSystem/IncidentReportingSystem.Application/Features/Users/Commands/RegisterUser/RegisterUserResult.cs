using System.Diagnostics.CodeAnalysis;

namespace IncidentReportingSystem.Application.Features.Users.Commands.RegisterUser
{
    public sealed record RegisterUserResult(
        Guid UserId,
        string Email,
        [property: SuppressMessage("Performance", "CA1819:Properties should not return arrays",
            Justification = "DTO boundary returning roles to client; acceptable.")]
        IReadOnlyCollection<string> Roles,
        DateTime CreatedAtUtc
    );
}
