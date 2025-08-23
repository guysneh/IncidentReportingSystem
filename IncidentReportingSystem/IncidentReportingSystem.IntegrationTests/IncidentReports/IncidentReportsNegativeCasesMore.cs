using IncidentReportingSystem.IntegrationTests.Utils;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace IncidentReportingSystem.IntegrationTests.IncidentReports;

public class IncidentReportsNegativeCasesMore : IClassFixture<Utils.CustomWebApplicationFactory>
{
    private readonly Utils.CustomWebApplicationFactory _factory;

    public IncidentReportsNegativeCasesMore(Utils.CustomWebApplicationFactory factory)
        => _factory = factory;
    [Fact]
    public async Task Post_CreateIncident_Unauthorized_WithoutToken()
    {
        var client = _factory.CreateClient();
        var payload = new
        {
            title = "title",
            description = "desc",
            category = "Other",
            severity = "Low"
        };
        var resp = await client.PostAsJsonAsync("/api/incidents", payload);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
