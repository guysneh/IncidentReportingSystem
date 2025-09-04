using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using IncidentReportingSystem.IntegrationTests.Utils;
using Xunit;

namespace IncidentReportingSystem.IntegrationTests.Cors;

[Trait("Category", "Integration")]
public class CorsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CorsTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Preflight_Allows_AllowedOrigin()
    {
        var client = _factory.CreateClient();
        var apiRoot = await ApiRootResolver.ResolveAsync(_factory, client);

        var req = new HttpRequestMessage(HttpMethod.Options, $"{apiRoot}/attachments/constraints");
        req.Headers.Add("Origin", "http://example.com");
        req.Headers.Add("Access-Control-Request-Method", "GET");

        var res = await client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.NoContent);
        res.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins).Should().BeTrue();
        origins!.Single().Should().Be("http://example.com");

        res.Headers.TryGetValues("Access-Control-Allow-Credentials", out var creds).Should().BeTrue();
        creds!.Single().Should().Be("true");

        res.Headers.TryGetValues("Access-Control-Allow-Methods", out var methods).Should().BeTrue();
        methods!.Single().ToUpperInvariant().Should().Contain("GET");

        res.Headers.TryGetValues("Vary", out var vary).Should().BeTrue();
        string.Join(",", vary!).ToLowerInvariant().Should().Contain("origin");
    }

    [Fact]
    public async Task Simple_Get_Adds_CorsHeaders_For_AllowedOrigin()
    {
        var client = _factory.CreateClient();
        var apiRoot = await ApiRootResolver.ResolveAsync(_factory, client);

        var req = new HttpRequestMessage(HttpMethod.Get, $"{apiRoot}/attachments/constraints");
        req.Headers.Add("Origin", "http://example.com");

        var res = await client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        res.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins).Should().BeTrue();
        origins!.Single().Should().Be("http://example.com");

        res.Headers.TryGetValues("Access-Control-Allow-Credentials", out var creds).Should().BeTrue();
        creds!.Single().Should().Be("true");

        res.Headers.TryGetValues("Access-Control-Expose-Headers", out var expose).Should().BeTrue();
        var exposed = expose!.Single().ToLowerInvariant();
        exposed.Should().Contain("etag");
        exposed.Should().Contain("content-disposition");
        exposed.Should().Contain("location");
        exposed.Should().Contain("x-correlation-id");

        var payload = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        payload.RootElement.TryGetProperty("maxSizeBytes", out _).Should().BeTrue();
    }

    [Fact]
    public async Task DisallowedOrigin_DoesNotAddCorsHeaders()
    {
        var client = _factory.CreateClient();
        var apiRoot = await ApiRootResolver.ResolveAsync(_factory, client);

        var req = new HttpRequestMessage(HttpMethod.Get, $"{apiRoot}/attachments/constraints");
        req.Headers.Add("Origin", "http://not-allowed.example");

        var res = await client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        res.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
        res.Headers.Contains("Access-Control-Allow-Credentials").Should().BeFalse();
    }
}
