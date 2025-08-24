namespace IncidentReportingSystem.IntegrationTests.Utils;

/// <summary>
/// Deterministic URL builder for the API in Integration tests:
///   <BasePath> + "/api/" + <version>
/// This matches a pipeline that uses UsePathBase(BasePath) while controllers route with "api/...".
/// </summary>
public static class ApiUrlHelper
{
    private static string Norm(string? s) => (s ?? string.Empty).Trim('/');

    private static string PickVersion(CustomWebApplicationFactory f)
    {
        var versions = (f.ApiVersions ?? Array.Empty<string>())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .ToArray();

        if (versions.Length == 0)
            return string.IsNullOrWhiteSpace(f.ApiVersionSegment) ? "v1" : f.ApiVersionSegment!;

        // Prefer dotted groups like "v1.0" if present, else highest lexical
        var dotted = versions.Where(v => v.Contains('.', StringComparison.Ordinal)).OrderByDescending(v => v).FirstOrDefault();
        return dotted ?? versions.OrderByDescending(v => v).First();
    }

    /// <summary>Builds the API root: &lt;basePath&gt;/api/&lt;version&gt; (no guessing).</summary>
    public static string ApiRoot(CustomWebApplicationFactory f)
    {
        var basePath = Norm(f.BasePath ?? "/");   // "", "api", "irs", "irs/api"
        var version = Norm(PickVersion(f));      // "v1" or "v1.0"
        // Always add an extra "api" segment, because controllers include "api/..." in routes.
        return "/" + string.Join('/', new[] { basePath, "api", version }.Where(s => !string.IsNullOrEmpty(s)));
    }

    /// <summary>Appends a relative route to ApiRoot.</summary>
    public static string Url(CustomWebApplicationFactory f, string relative)
    {
        var rel = relative.StartsWith('/') ? relative[1..] : relative;
        return $"{ApiRoot(f)}/{rel}";
    }
}
