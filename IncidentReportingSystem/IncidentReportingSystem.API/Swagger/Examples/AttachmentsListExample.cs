using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Any;

namespace IncidentReportingSystem.API.Swagger.Examples;

/// <summary>
/// Example response for listing attachments.
/// </summary>
public sealed class AttachmentsListExample : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.RelativePath?.Contains("attachments", StringComparison.OrdinalIgnoreCase) == true
            && context.ApiDescription.HttpMethod?.Equals("GET", StringComparison.OrdinalIgnoreCase) == true)
        {
            if (operation.Responses.TryGetValue("200", out var response) &&
                response.Content.TryGetValue("application/json", out var mediaType))
            {
                mediaType.Example = new OpenApiObject
                {
                    ["total"] = new OpenApiInteger(1),
                    ["skip"] = new OpenApiInteger(0),
                    ["take"] = new OpenApiInteger(100),
                    ["items"] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["id"] = new OpenApiString("b2c4f7c1-91b4-4e15-a5d3-8f9b6f8a1a6a"),
                            ["parentType"] = new OpenApiString("Incident"),
                            ["parentId"] = new OpenApiString("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                            ["fileName"] = new OpenApiString("error_screenshot.png"),
                            ["contentType"] = new OpenApiString("image/png"),
                            ["size"] = new OpenApiInteger(52342),
                            ["status"] = new OpenApiString("Completed"),
                            ["createdAt"] = new OpenApiString("2025-08-29T12:34:56Z"),
                            ["completedAt"] = new OpenApiString("2025-08-29T12:35:10Z"),
                            ["hasThumbnail"] = new OpenApiBoolean(false)
                        }
                    }
                };
            }
        }
    }
}

