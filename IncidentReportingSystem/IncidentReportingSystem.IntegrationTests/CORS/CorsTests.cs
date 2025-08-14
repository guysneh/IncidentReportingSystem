using System.Net;
using FluentAssertions;
using IncidentReportingSystem.IntegrationTests.Infrastructure;
using IncidentReportingSystem.IntegrationTests.Utils;

namespace IncidentReportingSystem.Tests.Integration.CORS;

public class CORSTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CORSTests(CustomWebApplicationFactory factory)
    {
        _client = AuthenticatedHttpClientFactory.CreateClientWithToken(factory);
    }

    private static string? GetHeader(HttpResponseMessage response, string headerName)
    {
        if (response.Headers.TryGetValues(headerName, out var values))
            return values.FirstOrDefault();

        if (response.Content.Headers.TryGetValues(headerName, out var contentValues))
            return contentValues.FirstOrDefault();

        return null;
    }

    private static void DumpHeaders(HttpResponseMessage response)
    {
        Console.WriteLine("=== Response Headers ===");
        foreach (var header in response.Headers)
            Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");

        foreach (var header in response.Content.Headers)
            Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");

        Console.WriteLine("========================");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CORSPolicy_Should_Allow_Configured_Origin_For_Get_Request()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/incidentreports");
        request.Headers.Add("Origin", "http://example.com");

        var response = await _client.SendAsync(request);

        DumpHeaders(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var allowOrigin = GetHeader(response, "Access-Control-Allow-Origin");
        allowOrigin.Should().NotBeNull("CORS should add ACAO when origin is allowed");
        allowOrigin.Should().Be("http://example.com");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CORSPolicy_Should_Respond_To_Preflight_Request_For_Configured_Origin()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/incidentreports");
        request.Headers.Add("Origin", "http://example.com");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type");

        var response = await _client.SendAsync(request);

        DumpHeaders(response);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var allowOrigin = GetHeader(response, "Access-Control-Allow-Origin");
        allowOrigin.Should().NotBeNull();
        allowOrigin.Should().Be("http://example.com");

        var allowMethods = GetHeader(response, "Access-Control-Allow-Methods");
        allowMethods.Should().NotBeNull();
        allowMethods.Should().Contain("POST");
    }
}
