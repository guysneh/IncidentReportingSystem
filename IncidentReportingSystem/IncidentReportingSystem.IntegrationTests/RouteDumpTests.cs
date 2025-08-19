using Microsoft.AspNetCore.Routing;
using IncidentReportingSystem.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;

public class RouteDumpTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public RouteDumpTests(CustomWebApplicationFactory f) => _factory = f;

    [Fact]
    public void Dump_All_Routes()
    {
        using var scope = _factory.Services.CreateScope();
        var ds = scope.ServiceProvider.GetRequiredService<EndpointDataSource>();
        var patterns = ds.Endpoints
            .OfType<RouteEndpoint>()
            .Select(e => e.RoutePattern.RawText)
            .OrderBy(p => p)
            .ToArray();

        // Write to test output (or Console)
        foreach (var p in patterns) Console.WriteLine(p);
        Assert.True(patterns.Length > 0);
    }
}
