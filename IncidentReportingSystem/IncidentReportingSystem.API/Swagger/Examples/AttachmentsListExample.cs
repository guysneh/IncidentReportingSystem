using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IncidentReportingSystem.API.Swagger.Examples
{
    /// <summary>
    /// Adds an example payload for the Attachments list endpoints,
    /// including new RBAC flags: canDelete and canDownload.
    /// Works for responses that return a paged object: { items: [...], total: N }.
    /// </summary>
    public sealed class AttachmentsListExample : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Heuristic: apply to list endpoints that include "attachments" in the route and are GETs.
            if (!string.Equals(operation?.RequestBody, null) || operation is null) return;
            if (operation.OperationId is null) return;

            // Only for 200 response with JSON
            if (!operation.Responses.TryGetValue("200", out var response)) return;
            if (!response.Content.ContainsKey("application/json")) return;

            // Build one attachment item example
            var item = new OpenApiObject
            {
                ["id"] = new OpenApiString("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                ["parentType"] = new OpenApiString("Incident"), // or "Comment"
                ["parentId"] = new OpenApiString("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                ["fileName"] = new OpenApiString("photo.jpg"),
                ["contentType"] = new OpenApiString("image/jpeg"),
                ["size"] = new OpenApiInteger(123456),
                ["status"] = new OpenApiString("Completed"),
                ["createdAt"] = new OpenApiString("2025-08-01T12:34:56Z"),
                ["completedAt"] = new OpenApiString("2025-08-01T12:35:30Z"),
                ["hasThumbnail"] = new OpenApiBoolean(true),

                // NEW: RBAC flags for UI
                ["canDelete"] = new OpenApiBoolean(true),
                ["canDownload"] = new OpenApiBoolean(true)
            };

            // Paged result: { items: [ item, ... ], total: N }
            var example = new OpenApiObject
            {
                ["items"] = new OpenApiArray { item },
                ["total"] = new OpenApiInteger(1)
            };

            response.Content["application/json"].Example = example;
        }
    }
}
