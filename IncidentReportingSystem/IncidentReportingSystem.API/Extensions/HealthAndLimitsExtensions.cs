using System;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

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

        // Health checks – single builder instance
        var hc = services.AddHealthChecks();

        if (env.IsEnvironment("Test"))
        {
            services.AddHealthChecks()
                    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "ready" });
        }
        else
        {
            services.AddHealthChecks()
                    .AddNpgSql(configuration["ConnectionStrings:DefaultConnection"]);
        }

        return services;
    }
}
