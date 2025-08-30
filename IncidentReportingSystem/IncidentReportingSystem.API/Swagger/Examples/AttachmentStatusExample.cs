using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IncidentReportingSystem.API.Swagger.Examples
{
    /// <summary>Enriches the /attachments/{id}/status schema with example values.</summary>
    public sealed class AttachmentStatusExample : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Safe heuristic: only for operations whose route ends with "/status"
            if (operation.OperationId is null || !operation.OperationId.Contains("Status", System.StringComparison.OrdinalIgnoreCase))
                return;

            if (!operation.Responses.TryGetValue("200", out var response)) return;
            if (!response.Content.ContainsKey("application/json")) return;

            response.Content["application/json"].Example = new OpenApiObject
            {
                ["status"] = new OpenApiString("Completed"),
                ["size"] = new OpenApiInteger(123456),
                ["existsInStorage"] = new OpenApiBoolean(true),
                ["contentType"] = new OpenApiString("image/jpeg")
            };
        }
    }
}
