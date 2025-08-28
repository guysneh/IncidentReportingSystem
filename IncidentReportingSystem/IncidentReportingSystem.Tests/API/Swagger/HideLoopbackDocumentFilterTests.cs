using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;
using IncidentReportingSystem.API.Swagger;

namespace IncidentReportingSystem.Tests.API.Swagger;

public sealed class HideLoopbackDocumentFilterTests
{
    [Fact]
    public void Removes_All_Loopback_Paths_And_Keeps_Normal_Ones()
    {
        var doc = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/api/v1/attachments/_loopback/upload"] = new OpenApiPathItem(),
                ["/api/v1/attachments/_loopback/download"] = new OpenApiPathItem(),
                ["/api/v1/incidentreports/{id}/attachments"] = new OpenApiPathItem()
            }
        };

        var filter = new HideLoopbackDocumentFilter();

        filter.Apply(doc, context: null!);

        Assert.DoesNotContain(doc.Paths.Keys, k => k.Contains("_loopback", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("/api/v1/incidentreports/{id}/attachments", doc.Paths.Keys);
    }

    [Fact]
    public void No_Loopback_Paths_No_Changes()
    {
        var doc = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/api/v1/incidentreports/{id}/attachments"] = new OpenApiPathItem(),
                ["/api/v1/comments/{id}/attachments"] = new OpenApiPathItem()
            }
        };

        var before = doc.Paths.Count;
        var filter = new HideLoopbackDocumentFilter();
        filter.Apply(doc, context: null!);

        Assert.Equal(before, doc.Paths.Count);
        Assert.All(doc.Paths.Keys, k => Assert.DoesNotContain("_loopback", k));
    }
}
