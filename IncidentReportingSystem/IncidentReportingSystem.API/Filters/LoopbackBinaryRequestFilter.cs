using IncidentReportingSystem.Infrastructure.Attachments;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IncidentReportingSystem.API.Filters;

public sealed class LoopbackBinaryRequestFilter : IOperationFilter
{
    private readonly string[] _allowedTypes;

    public LoopbackBinaryRequestFilter(IOptions<AttachmentOptions> options)
    {
        _allowedTypes = options.Value?.AllowedContentTypes?
            .Where(s => !string.IsNullOrWhiteSpace(s)).ToArray()
            ?? Array.Empty<string>();
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        try
        {
            var path = context.ApiDescription.RelativePath ?? string.Empty;
            var method = context.ApiDescription.HttpMethod ?? string.Empty;

            if (!method.Equals("PUT", StringComparison.OrdinalIgnoreCase)) return;
            if (!path.Contains("attachments/_loopback/upload", StringComparison.OrdinalIgnoreCase)) return;

            var schema = new OpenApiSchema { Type = "string", Format = "binary", Description = "Raw file bytes" };

            var content = new Dictionary<string, OpenApiMediaType>
            {
                // keep octet-stream as a generic option
                ["application/octet-stream"] = new OpenApiMediaType { Schema = schema }
            };

            // also expose allowed types, so the user can select the real one (image/jpeg, image/png, application/pdf)
            foreach (var ct in _allowedTypes)
                content[ct] = new OpenApiMediaType { Schema = schema };

            operation.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content = content
            };
        }
        catch
        {
            // never let Swagger generation crash
        }
    }
}
