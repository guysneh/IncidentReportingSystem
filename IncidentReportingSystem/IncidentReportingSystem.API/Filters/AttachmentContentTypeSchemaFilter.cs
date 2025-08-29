using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using IncidentReportingSystem.Infrastructure.Attachments;

namespace IncidentReportingSystem.API.Filters
{
    /// <summary>
    /// If a DTO has a 'contentType' property, projects allowed values from configuration as an enum.
    /// Never throws if options are missing/empty.
    /// </summary>
    public sealed class AttachmentContentTypeSchemaFilter : ISchemaFilter
    {
        private readonly string[] _allowedTypes;

        public AttachmentContentTypeSchemaFilter(IOptions<AttachmentOptions> options)
        {
            _allowedTypes = options.Value?.AllowedContentTypes?
                .Where(s => !string.IsNullOrWhiteSpace(s)).ToArray()
                ?? Array.Empty<string>();
        }

        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema?.Properties is null || schema.Properties.Count == 0)
                return;

            if (schema.Properties.TryGetValue("contentType", out var prop))
            {
                prop.Type = "string";
                prop.Format = null;

                if (_allowedTypes.Length > 0)
                {
                    prop.Enum = _allowedTypes.Select(v => (IOpenApiAny)new OpenApiString(v)).ToList();
                    prop.Example = new OpenApiString(_allowedTypes[0]);
                }
            }
        }
    }
}
