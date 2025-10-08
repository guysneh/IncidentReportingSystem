namespace IncidentReportingSystem.Application.Abstractions.Identity;

public interface IUserDirectory
{
    /// Resolves display names for the given user ids (GUIDs).
    Task<IDictionary<Guid, string>> ResolveDisplayNamesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken ct = default);
}
