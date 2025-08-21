using System.Threading.RateLimiting;

namespace IncidentReportingSystem.API.Extensions;

public static class HealthAndLimitsExtensions
{
    public static IServiceCollection AddHealthAndRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
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

        services.AddHealthChecks()
            .AddNpgSql(configuration["ConnectionStrings:DefaultConnection"]);

        return services;
    }
}
