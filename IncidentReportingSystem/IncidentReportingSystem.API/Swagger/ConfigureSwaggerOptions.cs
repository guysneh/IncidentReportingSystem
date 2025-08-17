using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;

namespace IncidentReportingSystem.API.Swagger;

public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    {
        _provider = provider;
    }

    public void Configure(SwaggerGenOptions options)
    {
        // Create a Swagger doc per API version
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(
                description.GroupName,
                new OpenApiInfo
                {
                    Title = $"Incident Reporting API {description.ApiVersion}",
                    Version = description.ApiVersion.ToString()
                });
        }

        // Key fix: include actions in the correct versioned doc,
        // and include non-versioned endpoints (e.g., minimal APIs) in the default doc (usually v1).
        options.DocInclusionPredicate((docName, apiDesc) =>
        {
            // Controllers with ApiVersion -> ApiExplorer sets GroupName (e.g., "v1")
            if (!string.IsNullOrEmpty(apiDesc.GroupName))
            {
                return string.Equals(apiDesc.GroupName, docName, StringComparison.OrdinalIgnoreCase);
            }

            // Minimal APIs or endpoints without ApiVersion metadata:
            // include them in the first (default) document.
            var defaultDoc = _provider.ApiVersionDescriptions.OrderBy(d => d.ApiVersion).First().GroupName;
            return string.Equals(docName, defaultDoc, StringComparison.OrdinalIgnoreCase);
        });
    }
}
