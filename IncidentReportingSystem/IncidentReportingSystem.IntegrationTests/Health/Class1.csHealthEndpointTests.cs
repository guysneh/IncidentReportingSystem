using System.Text.Json;
using FluentAssertions;
using IncidentReportingSystem.IntegrationTests.Utils;
using Xunit;

namespace IncidentReportingSystem.IntegrationTests.Health;

[Trait("Category", "Integration")]
public sealed class HealthEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public HealthEndpointTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact(DisplayName = "/health returns JSON with storage entry")]
    public async Task Health_Returns_Json_With_Storage_Entry()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/health");

        res.IsSuccessStatusCode.Should().BeTrue();
        res.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        root.TryGetProperty("status", out _).Should().BeTrue();
        root.TryGetProperty("checks", out var checks).Should().BeTrue();
        checks.ValueKind.Should().Be(JsonValueKind.Array);

        var hasStorage = checks.EnumerateArray()
            .Any(e => e.TryGetProperty("name", out var name) && name.GetString() == "storage");
        hasStorage.Should().BeTrue();
    }

    [Fact(DisplayName = "storage check is Healthy in Test")]
    public async Task Storage_Check_Should_Be_Healthy_In_Test()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/health");

        res.IsSuccessStatusCode.Should().BeTrue();

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var checks = doc.RootElement.GetProperty("checks").EnumerateArray();

        var storage = checks.First(e => e.GetProperty("name").GetString() == "storage");
        storage.GetProperty("status").GetString().Should().Be("Healthy");
    }
}
