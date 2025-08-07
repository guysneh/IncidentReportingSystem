using System.Net;
using FluentAssertions;
using System.Net.Http.Json;
using IncidentReportingSystem.IntegrationTests.Utils;

namespace IncidentReportingSystem.Tests.Integration.Smoke;

public class RateLimiterTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RateLimiterTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PostEndpoint_Should_Return_429_When_RateLimit_Exceeded()
    {
        // Arrange
        var client = AuthenticatedHttpClientFactory.CreateClientWithTokenAsync(_factory).GetAwaiter().GetResult();

        var tasks = new List<Task<HttpResponseMessage>>();
        const int requestCount = 20;

        // Act - Send many requests in parallel
        for (int i = 0; i < requestCount; i++)
        {
            tasks.Add(client.PostAsJsonAsync("/api/v1/incidentReports", new
            {
                Description = "Rate limit test",
                Location = "Berlin",
                ReporterId = Guid.NewGuid(),
                Category = "Security",
                SystemAffected = "VPN",
                Severity = "Medium"
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - At least one request should be rejected with 429 Too Many Requests
        tasks.Select(t => t.Result.StatusCode).Should().Contain(HttpStatusCode.TooManyRequests);
    }
}
