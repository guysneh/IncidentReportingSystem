using IncidentReportingSystem.Application.Abstractions.Identity;
using IncidentReportingSystem.Application.Abstractions.Persistence;

namespace IncidentReportingSystem.Infrastructure.Identity;

/// Resolves display names from the Users table via IUserRepository.
public sealed class DbUserDirectory : IUserDirectory
{
    private readonly IUserRepository _users;

    public DbUserDirectory(IUserRepository users)
    {
        _users = users;
    }

    public async Task<IDictionary<Guid, string>> ResolveDisplayNamesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken ct = default)
    {
        // Basic, safe implementation. For perf, you can add a batch method in IUserRepository later.
        var result = new Dictionary<Guid, string>();
        var distinct = userIds.Distinct().ToArray();

        foreach (var id in distinct)
        {
            var user = await _users.GetByIdAsync(id, ct).ConfigureAwait(false);
            if (user is null) continue;

            var display =
                string.Join(' ', new[] { user.FirstName, user.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();

            if (string.IsNullOrWhiteSpace(display))
                display = user.Email ?? id.ToString();

            // Use Email if names are empty; fallback to GUID string.
            result[id] = display;
        }

        return result;
    }
}
