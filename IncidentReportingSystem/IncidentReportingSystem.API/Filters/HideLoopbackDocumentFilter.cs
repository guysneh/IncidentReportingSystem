using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.RegularExpressions;

namespace IncidentReportingSystem.API.Filters
{
    public sealed class HideLoopbackDocumentFilter : IDocumentFilter
    {
        private static readonly Regex LoopbackSegment =
            new Regex(@"(^|/)_(?i:loopback)(/|$)", RegexOptions.Compiled);

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            if (swaggerDoc?.Paths is null || swaggerDoc.Paths.Count == 0)
                return;

            var filtered = new OpenApiPaths();

            // Snapshot KVPs to avoid any internal collection quirks
            foreach (var kvp in swaggerDoc.Paths.ToList())
            {
                var key = (kvp.Key ?? string.Empty).Trim(); // guard against stray whitespace
                if (LoopbackSegment.IsMatch(key))
                    continue; // drop any path that has a "_loopback" path segment

                filtered.Add(key, kvp.Value);
            }

            swaggerDoc.Paths = filtered;
        }
    }
}
