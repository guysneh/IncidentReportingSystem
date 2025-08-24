using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IncidentReportingSystem.API.Swagger;

public sealed class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;
    private readonly IConfiguration _cfg;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider, IConfiguration cfg)
    {
        _provider = provider;
        _cfg = cfg;
    }

    public void Configure(SwaggerGenOptions options)
    {
        var title = _cfg["App:Name"] ?? "Incident Reporting System API";

        foreach (var d in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(d.GroupName, new OpenApiInfo
            {
                Title = title,
                Version = d.ApiVersion.ToString(),
                Description = "REST API for Incident Reporting System." + (d.IsDeprecated ? " (DEPRECATED)" : "")
            });
        }

        // Never throw if XML files are missing
        TryXml(options, "IncidentReportingSystem.API");
        TryXml(options, "IncidentReportingSystem.Application");
        TryXml(options, "IncidentReportingSystem.Domain");
        TryXml(options, "IncidentReportingSystem.Infrastructure");

        // Map tricky CLR types
        options.MapType<Uri>(() => new OpenApiSchema { Type = "string", Format = "uri" });
        options.CustomSchemaIds(t => t.FullName!.Replace('+', '.'));
        options.SupportNonNullableReferenceTypes();
        options.UseInlineDefinitionsForEnums();
    }

    private static void TryXml(SwaggerGenOptions options, string asm)
    {
        var path = Path.Combine(AppContext.BaseDirectory, $"{asm}.xml");
        if (File.Exists(path))
            options.IncludeXmlComments(path, includeControllerXmlComments: true);
    }
}

