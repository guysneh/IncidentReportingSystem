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
                            var raw = ctx.Request.Path.Value ?? string.Empty;

                            // Normalize once: lowercase + trim trailing '/'
                            var path = raw.TrimEnd('/').ToLowerInvariant();

                            // Drop common health/infra noise
                            if (path == "/health" ||
                                path == "/healthz" ||
                                path.StartsWith("/health/") ||
                                path == "/ready" ||
                                path.StartsWith("/ready/") ||
                                path == "/live" ||
                                path.StartsWith("/live/") ||
                                path.StartsWith("/swagger") ||
                                path == "/favicon.ico" ||
                                path.StartsWith("/robots") ||
                                path.StartsWith("/metrics"))
                            {
                                return false;
                            }

                            // Optionally drop synthetic/keep-alive probes by User-Agent
                            var ua = ctx.Request.Headers.UserAgent.ToString().ToLowerInvariant();
                            if (!string.IsNullOrEmpty(ua) &&
                                (ua.Contains("alwayson") ||      // Azure App Service keep-alive
                                 ua.Contains("kube-probe") ||    // Kubernetes probes
                                 ua.Contains("availability") ||  // AI availability tests
                                 ua.Contains("uptime") ||        // uptime monitors
                                 ua.Contains("pingdom")))        // external pingers
                            {
                                return false;
                            }

                            return true;
                        };

                        o.EnrichWithHttpRequest = (activity, req) =>
                        {
                            if (req.Headers.TryGetValue("X-Correlation-Id", out var cid))
                                activity?.SetTag("correlation_id", cid.ToString());
                        };
                    });

                });

            return services;
        }
    }
}
