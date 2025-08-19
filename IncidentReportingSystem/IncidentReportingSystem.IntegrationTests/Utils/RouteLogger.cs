using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IncidentReportingSystem.IntegrationTests.Utils;

internal sealed class RouteLogger : IHostedService
{
    private readonly ILogger<RouteLogger> _logger;
    private readonly EndpointDataSource _endpoints;

    public RouteLogger(ILogger<RouteLogger> logger, EndpointDataSource endpoints)
    {
        _logger = logger;
        _endpoints = endpoints;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var e in _endpoints.Endpoints)
        {
            if (e is RouteEndpoint re)
            {
                _logger.LogInformation("[TEST HOST] Mapped endpoint: {Route}",
                    re.RoutePattern.RawText);
            }
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
