namespace IncidentReportingSystem.Application.Abstractions.Security;

/// <summary>
/// Provides the identity of the authenticated caller from the execution context.
/// Implementations must not depend on EF or any persistence concerns.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Returns the authenticated user's identifier as <see cref="Guid"/>.
    /// Throws <see cref="InvalidOperationException"/> if no authenticated user is present.
    /// </summary>
    Guid UserIdOrThrow();
}
