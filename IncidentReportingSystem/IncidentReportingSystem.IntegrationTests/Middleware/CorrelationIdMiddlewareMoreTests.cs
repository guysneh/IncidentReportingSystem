using System.Net;
using Xunit;

namespace IncidentReportingSystem.IntegrationTests.Middleware;

public class CorrelationIdMiddlewareMoreTests : IClassFixture<Utils.CustomWebApplicationFactory>
{
    private const string HeaderName = "X-Correlation-ID";
    private readonly Utils.CustomWebApplicationFactory _factory;

    public CorrelationIdMiddlewareMoreTests(Utils.CustomWebApplicationFactory factory)
        => _factory = factory;

    [Fact]
    public async Task When_Header_Missing_Server_Generates_It()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/health/live"); 
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.True(resp.Headers.Contains(HeaderName));
        var value = string.Join(",", resp.Headers.GetValues(HeaderName));
        Assert.False(string.IsNullOrWhiteSpace(value));
    }

    [Fact]
    public async Task When_Header_Present_Server_Echos_It()
    {
        var client = _factory.CreateClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        req.Headers.TryAddWithoutValidation(HeaderName, "test-corr-id-123");
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var echo = string.Join(",", resp.Headers.GetValues(HeaderName));
        Assert.Equal("test-corr-id-123", echo);
    }
}
