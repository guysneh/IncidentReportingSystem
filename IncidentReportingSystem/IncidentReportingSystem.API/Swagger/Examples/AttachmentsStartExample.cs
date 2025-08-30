using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;

namespace IncidentReportingSystem.API.Swagger.Examples
{
    /// <summary>
    /// Adds an OpenAPI example for the Start Upload endpoints so UI developers
    /// can see the method and required headers without guessing.
    /// Applies to:
    ///  - POST /api/v{version}/incidentreports/{id}/attachments/start
    ///  - POST /api/v{version}/comments/{id}/attachments/start
    /// </summary>
    public sealed class AttachmentsStartExample : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var path = context.ApiDescription.RelativePath ?? string.Empty;
            if (!path.Contains("attachments/start", StringComparison.OrdinalIgnoreCase))
                return;

            if (!operation.Responses.TryGetValue("200", out var ok)) return;
            if (!ok.Content.TryGetValue("application/json", out var json)) return;

            // Example payload mirrors StartUploadAttachmentResponse (non-breaking addition of method/headers)
            var attachmentId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

            json.Example = new OpenApiObject
            {
                ["attachmentId"] = new OpenApiString(attachmentId.ToString()),
                ["uploadUrl"] = new OpenApiString($"https://localhost/api/v1/attachments/_loopback/upload?path=incidents/{{incidentId}}/{attachmentId}/photo.png"),
                ["storagePath"] = new OpenApiString("incidents/{incidentId}/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/photo.png"),
                ["method"] = new OpenApiString("PUT"),
                // Loopback requires no headers; in Azure this might include: { "x-ms-blob-type": "BlockBlob" }
                ["headers"] = new OpenApiObject()
            };
        }
    }
}
