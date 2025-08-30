using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Any;

namespace IncidentReportingSystem.API.Swagger.Examples;

public sealed class CommentsListExample : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.RelativePath?.Contains("comments", StringComparison.OrdinalIgnoreCase) == true
            && context.ApiDescription.HttpMethod?.Equals("GET", StringComparison.OrdinalIgnoreCase) == true)
        {
            if (operation.Responses.TryGetValue("200", out var response) &&
                response.Content.TryGetValue("application/json", out var mediaType))
            {
                mediaType.Example = new OpenApiObject
                {
                    ["total"] = new OpenApiInteger(2),
                    ["skip"] = new OpenApiInteger(0),
                    ["take"] = new OpenApiInteger(50),
                    ["items"] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["id"] = new OpenApiString("4a65f64d-1111-4562-b3fc-2c963f66aaa1"),
                            ["incidentId"] = new OpenApiString("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                            ["userId"] = new OpenApiString("5b77f64d-9999-4562-b3fc-2c963f66bbb2"),
                            ["text"] = new OpenApiString("First comment on the incident."),
                            ["createdAtUtc"] = new OpenApiString("2025-08-29T12:36:56Z")
                        }
                    }
                };
            }
        }
    }
}
