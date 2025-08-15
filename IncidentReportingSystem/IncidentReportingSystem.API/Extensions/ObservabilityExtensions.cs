using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Trace;                        

namespace IncidentReportingSystem.API.Extensions 
{
    public static class ObservabilityExtensions
    {
        /// <summary>
        /// Minimal OpenTelemetry → Azure Monitor (Application Insights).
        /// Opt-in: activates only if APPLICATIONINSIGHTS_CONNECTION_STRING exists.
        /// </summary>
        public static IServiceCollection AddAppTelemetry(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment env)
        {
            var connectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // Skip telemetry when no connection string (e.g., test runs).
                return services;
            }

            services.AddOpenTelemetry()
                .UseAzureMonitor() // reads APPLICATIONINSIGHTS_CONNECTION_STRING automatically
                .WithTracing(b =>
                {
                    b.AddAspNetCoreInstrumentation(o =>
                    {
                        o.RecordException = true;
                        o.Filter = ctx =>
                        {
                            var path = ctx.Request.Path.Value ?? string.Empty;
                            // reduce noise
                            return !path.StartsWith("/health") && !path.StartsWith("/swagger");
                        };
                        o.EnrichWithHttpRequest = (activity, req) =>
                        {
                            if (req.Headers.TryGetValue("X-Correlation-Id", out var cid))
                                activity?.SetTag("correlation_id", cid.ToString());
                        };
                    });

                    b.AddHttpClientInstrumentation(o =>
                    {
                        o.RecordException = true;
                    });
                });

            return services;
        }
    }
}
