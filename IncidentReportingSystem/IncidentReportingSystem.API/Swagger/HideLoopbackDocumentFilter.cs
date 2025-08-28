using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IncidentReportingSystem.API.Swagger;

public sealed class HideLoopbackDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        if (swaggerDoc?.Paths is null || swaggerDoc.Paths.Count == 0)
            return;

        var filtered = new OpenApiPaths();
        foreach (var kvp in swaggerDoc.Paths.ToList()) // snapshot
        {
            var key = kvp.Key ?? string.Empty;
            if (key.IndexOf("/_loopback", StringComparison.OrdinalIgnoreCase) >= 0)
                continue;

            filtered.Add(key, kvp.Value);
        }

        swaggerDoc.Paths = filtered;
    }
}
