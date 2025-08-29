using IncidentReportingSystem.API.Contracts.Authentication;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IncidentReportingSystem.API.Swagger.Examples;

/// <summary>
/// Example response for WhoAmI endpoint.
/// </summary>
public sealed class WhoAmIExample : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.RelativePath?.Contains("auth/me", StringComparison.OrdinalIgnoreCase) == true)
        {
            operation.Responses["200"].Content["application/json"].Example =
                new Microsoft.OpenApi.Any.OpenApiObject
                {
                    ["userId"] = new Microsoft.OpenApi.Any.OpenApiString("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                    ["email"] = new Microsoft.OpenApi.Any.OpenApiString("jane.doe@example.com"),
                    ["roles"] = new Microsoft.OpenApi.Any.OpenApiArray
                    {
                        new Microsoft.OpenApi.Any.OpenApiString("User")
                    },
                    ["firstName"] = new Microsoft.OpenApi.Any.OpenApiString("Jane"),
                    ["lastName"] = new Microsoft.OpenApi.Any.OpenApiString("Doe"),
                    ["displayName"] = new Microsoft.OpenApi.Any.OpenApiString("Jane Doe")
                };
        }
    }
}
