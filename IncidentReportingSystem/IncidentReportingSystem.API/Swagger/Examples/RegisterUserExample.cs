using IncidentReportingSystem.Application.Features.Users.Commands.RegisterUser;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IncidentReportingSystem.API.Swagger.Examples;

/// <summary>
/// Example payload for registering a user.
/// </summary>
public sealed class RegisterUserExample : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.RelativePath?.Contains("auth/register", StringComparison.OrdinalIgnoreCase) == true)
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content =
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Example = new Microsoft.OpenApi.Any.OpenApiObject
                        {
                            ["email"] = new Microsoft.OpenApi.Any.OpenApiString("jane.doe@example.com"),
                            ["password"] = new Microsoft.OpenApi.Any.OpenApiString("P@ssw0rd!"),
                            ["roles"] = new Microsoft.OpenApi.Any.OpenApiArray
                            {
                                new Microsoft.OpenApi.Any.OpenApiString("User")
                            },
                            ["firstName"] = new Microsoft.OpenApi.Any.OpenApiString("Jane"),
                            ["lastName"] = new Microsoft.OpenApi.Any.OpenApiString("Doe")
                        }
                    }
                }
            };
        }
    }
}
