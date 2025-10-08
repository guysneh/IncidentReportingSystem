using IncidentReportingSystem.Application.Abstractions.Identity;

namespace IncidentReportingSystem.Infrastructure.Identity;

/// Adapts the infrastructure IUserDirectory to the application abstraction.
public sealed class UserDirectoryAdapter : IncidentReportingSystem.Application.Abstractions.Identity.IUserDirectory
{
    private readonly IUserDirectory _inner;

    public UserDirectoryAdapter(IUserDirectory inner)
    {
        _inner = inner;
    }

    public Task<IDictionary<Guid, string>> ResolveDisplayNamesAsync(IEnumerable<Guid> userIds, CancellationToken ct = default)
        => _inner.ResolveDisplayNamesAsync(userIds, ct);
}
