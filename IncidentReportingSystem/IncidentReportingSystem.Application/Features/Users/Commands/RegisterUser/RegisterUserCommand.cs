using System.Diagnostics.CodeAnalysis;
using MediatR;

namespace IncidentReportingSystem.Application.Features.Users.Commands.RegisterUser
{
    /// <summary>
    /// Command to register a new application user with credentials, roles,
    /// and optional profile fields.
    /// </summary>
    public sealed record RegisterUserCommand(
        string Email,
        string Password,
        [property: SuppressMessage("Performance", "CA1819:Properties should not return arrays",
            Justification = "DTO boundary: array is acceptable and mapped to Postgres text[]")]
        string[] Roles,
        string? FirstName = null,
        string? LastName = null
    ) : IRequest<RegisterUserResult>;

}
