using IncidentReportingSystem.Domain.Entities;

namespace IncidentReportingSystem.Application.Abstractions.Persistence;
/// <summary>
/// Persistence abstraction for Incident comments. No EF Core types should leak here.
/// Implementations live in Infrastructure.
/// </summary>
public interface IIncidentCommentsRepository
{
    /// <summary>Returns true if the incident exists.</summary>
    Task<bool> IncidentExistsAsync(Guid incidentId, CancellationToken ct);

    /// <summary>Adds a new comment entity to the persistence context.</summary>
    Task<IncidentComment> AddAsync(IncidentComment comment, CancellationToken ct);

    /// <summary>Fetches a single comment for a given incident.</summary>
    Task<IncidentComment?> GetAsync(Guid incidentId, Guid commentId, CancellationToken ct);

    /// <summary>Removes a comment from the persistence context.</summary>
    Task RemoveAsync(IncidentComment comment, CancellationToken ct);

    /// <summary>Lists comments for an incident, newest-first with pagination.</summary>
    Task<IReadOnlyList<IncidentComment>> ListAsync(Guid incidentId, int skip, int take, CancellationToken ct);
}