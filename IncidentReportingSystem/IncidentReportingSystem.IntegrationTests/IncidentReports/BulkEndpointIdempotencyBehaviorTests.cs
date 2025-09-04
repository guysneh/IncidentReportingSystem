using IncidentReportingSystem.IntegrationTests.Utils;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit.Abstractions;

namespace IncidentReportingSystem.IntegrationTests.IncidentReports;

public class BulkEndpointIdempotencyBehaviorTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public BulkEndpointIdempotencyBehaviorTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    private static readonly JsonSerializerOptions JsonEnumOptions =
        new() { Converters = { new JsonStringEnumConverter() } };

    [Fact]
    public async Task SameKeySamePayload_Returns_Same_Response()
    {
        var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_factory, roles: new[] { "Admin" });

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var body = new { ids = new[] { id1, id2 }, newStatus = "Closed" };

        using var req1 = new HttpRequestMessage(HttpMethod.Post, RouteHelper.R(_factory, "IncidentReports/bulk-status"))
        { Content = JsonContent.Create(body /*, options: JsonEnumOptions */) };
        req1.Headers.Add("Idempotency-Key", "bulk-test-1");
        var res1 = await client.SendAsync(req1);
        _output.WriteLine(await res1.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, res1.StatusCode);
        var payload1 = await res1.Content.ReadFromJsonAsync<BulkResult>();

        using var req2 = new HttpRequestMessage(HttpMethod.Post, RouteHelper.R(_factory, "IncidentReports/bulk-status"))
        { Content = JsonContent.Create(body /*, options: JsonEnumOptions */) };
        req2.Headers.Add("Idempotency-Key", "bulk-test-1");
        var res2 = await client.SendAsync(req2);
        _output.WriteLine(await res2.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, res2.StatusCode);
        var payload2 = await res2.Content.ReadFromJsonAsync<BulkResult>();

        Assert.Equal(payload1!.Updated, payload2!.Updated);
        Assert.Equal(payload1.NotFound, payload2.NotFound);
        Assert.Equal("bulk-test-1", payload2.IdempotencyKey);
    }

    [Fact]
    public async Task SameKeyDifferentPayload_FirstWriteWins_Returns_Original()
    {
        var client = AuthenticatedHttpClientFactory.CreateClientWithToken(_factory, roles: new[] { "Admin" });

        var first = new { ids = new[] { Guid.NewGuid() }, newStatus = "InProgress" };
        var second = new { ids = new[] { Guid.NewGuid(), Guid.NewGuid() }, newStatus = "Closed" };

        using var r1 = new HttpRequestMessage(HttpMethod.Post, RouteHelper.R(_factory, "IncidentReports/bulk-status"))
        { Content = JsonContent.Create(first /*, options: JsonEnumOptions */) };
        r1.Headers.Add("Idempotency-Key", "bulk-test-2");
        var res1 = await client.SendAsync(r1);
        _output.WriteLine(await res1.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, res1.StatusCode);
        var p1 = await res1.Content.ReadFromJsonAsync<BulkResult>();

        using var r2 = new HttpRequestMessage(HttpMethod.Post, RouteHelper.R(_factory, "IncidentReports/bulk-status"))
        { Content = JsonContent.Create(second /*, options: JsonEnumOptions */) };
        r2.Headers.Add("Idempotency-Key", "bulk-test-2");
        var res2 = await client.SendAsync(r2);
        _output.WriteLine(await res2.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, res2.StatusCode);
        var p2 = await res2.Content.ReadFromJsonAsync<BulkResult>();

        Assert.Equal(p1!.Updated, p2!.Updated);
        Assert.Equal(p1.NotFound, p2.NotFound);
        Assert.Equal("bulk-test-2", p2.IdempotencyKey);
    }

    private sealed class BulkResult
    {
        public int Updated { get; set; }
        public Guid[] NotFound { get; set; } = Array.Empty<Guid>();
        public string IdempotencyKey { get; set; } = string.Empty;
    }
}
