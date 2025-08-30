using IncidentReportingSystem.API.Health;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.RateLimiting;

namespace IncidentReportingSystem.API.Extensions;
public static class HealthAndRateLimitingExtensions
{
    public static IServiceCollection AddHealthAndRateLimiting(
    this IServiceCollection services,
    IConfiguration configuration,
    IHostEnvironment env)
    {
        // Rate limiter (unchanged)
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                RateLimitPartition.GetFixedWindowLimiter("default", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromSeconds(10),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 5,
                    AutoReplenishment = true
                }));
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        // Health checks — single builder
        var hc = services.AddHealthChecks();

        if (env.IsEnvironment("Test"))
        {
            hc.AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "ready" });
        }
        else
        {
            hc.AddNpgSql(configuration["ConnectionStrings:DefaultConnection"]);
        }

        // Optional storage check (Non-prod)
        if (!env.IsProduction())
        {
            hc.AddCheck<AttachmentStorageHealthCheck>(
                "storage",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "ready" });
        }

        return services;
    }
}
