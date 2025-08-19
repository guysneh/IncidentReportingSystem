using System.Collections.Concurrent;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace IncidentReportingSystem.IntegrationTests.Utils
{
    internal static class RouteHelper
    {
        // Cache per segment -> discovered prefix (e.g., "", "/api", "/api/v1.0")
        private static readonly ConcurrentDictionary<string, string> PrefixCache = new();

        /// <summary>
        /// Build URL by discovering the actual prefix (base + api-version) from EndpointDataSource.
        /// No assumptions: if controllers are unversioned, returns "/{relative}";
        /// if they're under "/api/v1.0", returns "/api/v1.0/{relative}".
        /// </summary>
        public static string R(CustomWebApplicationFactory f, string relative)
        {
            if (f is null) throw new ArgumentNullException(nameof(f));
            relative ??= string.Empty;

            var segment = FirstSegment(relative);
            var prefix = ResolvePrefixForSegment(f, segment);
            var path = $"{prefix}/{relative}";
            return Normalize(path);
        }

        // Keep V/U for legacy calls if you still have some in code. They delegate to R.
        public static string V(CustomWebApplicationFactory f, string relative) => R(f, relative);
        public static string U(CustomWebApplicationFactory f, string relative) => R(f, relative);

        // ------------ internals ------------

        private static string FirstSegment(string relative)
        {
            var r = (relative ?? string.Empty).TrimStart('/');
            var q = r.IndexOf('?');
            if (q >= 0) r = r[..q];
            var slash = r.IndexOf('/');
            return (slash >= 0 ? r[..slash] : r).ToLowerInvariant();
        }

        private static string Normalize(string path)
        {
            var p = (path ?? string.Empty).Replace("//", "/");
            if (!p.StartsWith("/")) p = "/" + p;
            return p;
        }

        private static string ResolvePrefixForSegment(CustomWebApplicationFactory f, string segment)
        {
            if (string.IsNullOrWhiteSpace(segment)) return string.Empty;

            var key = segment; // prefix is discovered from endpoints, not from config
            return PrefixCache.GetOrAdd(key, _ =>
            {
                using var scope = f.Services.CreateScope();
                var ds = scope.ServiceProvider.GetRequiredService<EndpointDataSource>();
                var patterns = ds.Endpoints.OfType<RouteEndpoint>()
                    .Select(e => e.RoutePattern.RawText)
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p!.Trim())
                    .ToArray();

                // Work in lowercase for matching
                var lower = patterns.Select(p => p.ToLowerInvariant()).ToArray();
                var seg = "/" + segment.ToLowerInvariant();

                // Collect all prefixes that end right before "/{segment}"
                // Example hits:
                //   ""                         + "/incidentreports"
                //   "/api"                     + "/incidentreports"
                //   "/api/v1.0"                + "/incidentreports"
                //   "/api/v{version:apiversion}"+ "/incidentreports"  (we'll coerce this to a concrete "v1" later if needed)
                var candidates = lower
                    .Select(p => (p, idx: p.IndexOf(seg, StringComparison.Ordinal)))
                    .Where(t => t.idx >= 0)
                    .Select(t => t.p[..t.idx]) // prefix part before "/segment"
                    .Distinct()
                    .ToList();

                if (candidates.Count == 0)
                    return string.Empty; // fall back: no prefix found → unversioned

                // Prefer the longest concrete prefix (so "/api/v1.0" wins over "/api").
                // If only template version exists (e.g., "/api/v{version:apiVersion}"), fallback to "/api/v1".
                var longest = candidates
                    .OrderByDescending(s => s.Length)
                    .First();

                // If prefix contains "{version:apiVersion}" template, coerce to "v1" (safe default).
                if (longest.Contains("{version:apiversion}", StringComparison.OrdinalIgnoreCase))
                {
                    longest = longest.Replace("{version:apiversion}", "1", StringComparison.OrdinalIgnoreCase);
                    if (!longest.Contains("/v", StringComparison.OrdinalIgnoreCase))
                        longest = longest.Replace("/v", "/v1", StringComparison.OrdinalIgnoreCase);
                    if (!longest.Contains("/v1", StringComparison.OrdinalIgnoreCase))
                        longest = longest.TrimEnd('/') + "/v1";
                }

                // Normalize slashes and ensure leading slash
                var prefix = Normalize(longest);
                return prefix == "/" ? string.Empty : prefix.TrimEnd('/');
            });
        }
    }
}
