using System.Collections.Concurrent;

namespace IncidentReportingSystem.IntegrationTests.Utils;

/// <summary>
/// Resolves the live API root by probing GET <root>/attachments/constraints.
/// Robust to BasePath (with/without "/api") and version groups ("v1" or "v1.0").
/// Result is memoized per test run.
/// </summary>
public static class ApiRootResolver
{
    private static readonly ConcurrentDictionary<string, string> Cache = new();

    public static async Task<string> ResolveAsync(CustomWebApplicationFactory f, HttpClient client)
    {
        var key = $"{f.BasePath}|{string.Join(",", f.ApiVersions ?? Array.Empty<string>())}|{f.ApiVersionSegment}";
        if (Cache.TryGetValue(key, out var cached))
            return cached;

        // Candidate versions: reported groups + Program's default + common fallbacks
        var versions = new List<string>();
        if (f.ApiVersions is { Count: > 0 }) versions.AddRange(f.ApiVersions);
        if (!string.IsNullOrWhiteSpace(f.ApiVersionSegment)) versions.Add(f.ApiVersionSegment!);
        versions.AddRange(new[] { "v1.0", "v1" }); // include both orders
        versions = versions.Select(v => v.Trim('/')).Distinct().ToList();

        // Candidate base prefixes: no base + configured BasePath (e.g. "/api")
        var basePath = (f.BasePath ?? "/").TrimEnd('/');
        var bases = new List<string> { "" };
        if (!string.IsNullOrEmpty(basePath) && basePath != "/")
            bases.Add(basePath); // e.g., "/api"

        // Compose candidates like: "/api/v1", "/api/api/v1", etc.
        var roots = new List<string>();
        foreach (var v in versions)
        {
            roots.Add($"/api/{v}");
            foreach (var b in bases.Where(b => !string.IsNullOrEmpty(b)))
                roots.Add($"{b}/api/{v}");
        }

        foreach (var r in roots.Distinct())
        {
            var url = $"{r}/attachments/constraints";
            var res = await client.GetAsync(url);
            if (res.IsSuccessStatusCode)
            {
                Cache[key] = r;
                return r;
            }
        }

        throw new InvalidOperationException("Could not resolve API root for attachments by probing constraints endpoint.");
    }
}
