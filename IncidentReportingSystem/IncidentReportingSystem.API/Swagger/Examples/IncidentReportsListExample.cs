using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Any;

namespace IncidentReportingSystem.API.Swagger.Examples;

public sealed class IncidentReportsListExample : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.RelativePath?.EndsWith("incidentreports", StringComparison.OrdinalIgnoreCase) == true
            && context.ApiDescription.HttpMethod?.Equals("GET", StringComparison.OrdinalIgnoreCase) == true)
        {
            if (operation.Responses.TryGetValue("200", out var response) &&
                response.Content.TryGetValue("application/json", out var mediaType))
            {
                mediaType.Example = new OpenApiObject
                {
                    ["total"] = new OpenApiInteger(1),
                    ["skip"] = new OpenApiInteger(0),
                    ["take"] = new OpenApiInteger(50),
                    ["items"] = new OpenApiArray
                    {
                        new OpenApiObject
                        {
                            ["id"] = new OpenApiString("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                            ["description"] = new OpenApiString("Power outage in datacenter"),
                            ["location"] = new OpenApiString("Berlin"),
                            ["reporterId"] = new OpenApiString("d2f9a555-7b22-4e2b-92dd-4eafc3ab37fa"),
                            ["category"] = new OpenApiString("Electrical"),
                            ["systemAffected"] = new OpenApiString("Backend"),
                            ["severity"] = new OpenApiString("High"),
                            ["reportedAt"] = new OpenApiString("2025-08-29T12:34:56Z"),
                            ["status"] = new OpenApiString("Open"),
                            ["createdAt"] = new OpenApiString("2025-08-29T12:35:10Z")
                        }
                    }
                };
            }
        }
    }
}
