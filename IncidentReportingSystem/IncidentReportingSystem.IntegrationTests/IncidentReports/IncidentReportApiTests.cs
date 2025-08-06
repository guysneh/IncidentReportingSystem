using System.Net;
using FluentAssertions;
using IncidentReportingSystem.IntegrationTests.TestUtils;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IncidentReportingSystem.IntegrationTests.IncidentReports;

public class IncidentReportApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public IncidentReportApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetIncidentReports_ShouldReturnSuccess()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/incidentReports");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
