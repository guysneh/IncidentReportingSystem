using System;
using System.Linq;
using IncidentReportingSystem.Infrastructure.Attachments;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IncidentReportingSystem.API.Swagger
{
    /// <summary>
    /// Adds an enum (dropdown) to StartUploadBody.ContentType based on AttachmentOptions.AllowedContentTypes.
    /// </summary>
    public sealed class AttachmentContentTypeSchemaFilter : ISchemaFilter
    {
        private readonly IOptions<AttachmentOptions> _options;
        public AttachmentContentTypeSchemaFilter(IOptions<AttachmentOptions> options) => _options = options;

        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema?.Properties == null) return;

            // Match the API body model name (nested type)
            var isStartUploadBody = context.Type.FullName?.EndsWith("AttachmentsController+StartUploadBody", StringComparison.Ordinal) == true;
            if (!isStartUploadBody) return;

            if (schema.Properties.TryGetValue("contentType", out var ctSchema))
            {
                var allowed = _options.Value.AllowedContentTypes?.ToArray() ?? Array.Empty<string>();
                if (allowed.Length > 0)
                {
                    ctSchema.Enum = allowed.Select(v => (IOpenApiAny)new OpenApiString(v)).ToList();
                    ctSchema.Example = new OpenApiString(allowed[0]);
                    ctSchema.Description = (ctSchema.Description ?? string.Empty) + " (Allowed values)";
                }
            }
        }
    }
}
