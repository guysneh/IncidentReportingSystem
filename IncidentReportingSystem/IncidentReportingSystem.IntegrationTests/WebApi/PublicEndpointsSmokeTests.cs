using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using IncidentReportingSystem.IntegrationTests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace IncidentReportingSystem.IntegrationTests.WebApi;

[Collection("BlobStorageSerial")]
public class PublicEndpointsSmokeTests : IClassFixture<AttachmentsWebApplicationFactory>
{
    private readonly AttachmentsWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;
    private readonly HttpClient _anon;

    public PublicEndpointsSmokeTests(AttachmentsWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
        _anon = factory.CreateClient(); 
    }

    [Fact(DisplayName = "Swagger JSON loads (status 200 and has 'openapi')")]
    public async Task SwaggerJson_Loads()
    {
        var resp = await _anon.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("openapi", out _));
    }

    [Fact(DisplayName = "Health endpoints return 200")]
    public async Task Health_Endpoints()
    {
        var live = await _anon.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);

        var ready = await _anon.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }

    [Fact(DisplayName = "Robots endpoints respond")]
    public async Task Robots()
    {
        var robots = await _anon.GetAsync("/robots.txt");
        Assert.Equal(HttpStatusCode.OK, robots.StatusCode);

        var probe = await _anon.GetAsync("/robots933456.txt");
        Assert.Equal(HttpStatusCode.OK, probe.StatusCode);
    }

    [Fact(DisplayName = "CORS preflight (OPTIONS) returns 204 for allowed origin")]
    public async Task Cors_Preflight_204()
    {
        // The default test appsettings allow http://example.com
        using var req = new HttpRequestMessage(HttpMethod.Options, "/api/v1/config-demo");
        req.Headers.Add("Origin", "http://example.com");
        req.Headers.Add("Access-Control-Request-Method", "GET");

        var resp = await _anon.SendAsync(req);
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact(DisplayName = "Versioned demo endpoint returns Enabled=true and OK")]
    public async Task Versioned_Demo()
    {
        var resp = await _anon.GetAsync("/api/v1/config-demo");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("Enabled").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(doc.RootElement.GetProperty("ApiVersion").GetString()));
    }
}
