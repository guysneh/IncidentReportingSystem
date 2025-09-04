using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Any;

namespace IncidentReportingSystem.API.Swagger.Examples;

public sealed class CommentsListExample : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.RelativePath?.Contains("/comments", StringComparison.OrdinalIgnoreCase) == true
            && context.ApiDescription.HttpMethod?.Equals("GET", StringComparison.OrdinalIgnoreCase) == true
            && operation.Responses.TryGetValue("200", out var resp)
            && resp.Content.TryGetValue("application/json", out var media))
        {
            media.Example = new OpenApiObject
            {
                ["total"] = new OpenApiInteger(2),
                ["skip"] = new OpenApiInteger(0),
                ["take"] = new OpenApiInteger(50),
                ["items"] = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        ["id"]          = new OpenApiString("11111111-1111-1111-1111-111111111111"),
                        ["incidentId"]  = new OpenApiString("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                        ["userId"]      = new OpenApiString("22222222-2222-2222-2222-222222222222"),
                        ["text"]        = new OpenApiString("First comment."),
                        ["createdAtUtc"]= new OpenApiString("2025-08-29T12:36:56Z")
                    }
                }
            };
        }
    }
}
