using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Any;

namespace IncidentReportingSystem.API.Swagger.Examples;

/// <summary>
/// Example error response using ProblemDetails.
/// </summary>
public sealed class ProblemDetailsExample : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        foreach (var resp in operation.Responses)
        {
            if (resp.Key.StartsWith("4"))
            {
                resp.Value.Content["application/problem+json"] = new OpenApiMediaType
                {
                    Example = new OpenApiObject
                    {
                        ["type"] = new OpenApiString("https://httpstatuses.com/400"),
                        ["title"] = new OpenApiString("Bad Request"),
                        ["status"] = new OpenApiInteger(400),
                        ["detail"] = new OpenApiString("Email format is invalid."),
                        ["instance"] = new OpenApiString("/api/v1/auth/register"),
                        ["traceId"] = new OpenApiString("0HNF3PTOF38BF:00000015")
                    }
                };
            }
        }
    }
}
