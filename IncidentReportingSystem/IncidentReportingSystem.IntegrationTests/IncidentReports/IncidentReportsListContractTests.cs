using System.Net;
using System.Net.Http.Json;
using IncidentReportingSystem.IntegrationTests.Utils;
using Xunit;

namespace IncidentReportingSystem.IntegrationTests.Incidents;

[Trait("Category", "Integration")]
public sealed class IncidentReportsListContractTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public IncidentReportsListContractTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact(DisplayName = "GET /incidentreports returns PagedResponse contract")]
    public async Task List_Contract_Is_Paged()
    {
        var client = _factory.AsUser();
        var res = await client.GetAsync(RouteHelper.R(_factory, "incidentreports"));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var json = await res.Content.ReadAsStringAsync();
        Assert.Contains("\"total\"", json);
        Assert.Contains("\"items\"", json);
        Assert.Contains("\"skip\"", json);
        Assert.Contains("\"take\"", json);
    }
}
