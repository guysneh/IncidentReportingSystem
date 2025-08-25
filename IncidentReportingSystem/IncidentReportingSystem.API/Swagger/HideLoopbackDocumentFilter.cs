using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IncidentReportingSystem.API.Swagger;

public sealed class HideLoopbackDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var keys = swaggerDoc.Paths.Keys
            .Where(k => k.Contains("/attachments/_loopback/upload", StringComparison.OrdinalIgnoreCase))
            .ToList();
        foreach (var k in keys) swaggerDoc.Paths.Remove(k);
    }
}
