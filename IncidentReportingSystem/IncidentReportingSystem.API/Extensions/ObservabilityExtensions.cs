using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;

namespace IncidentReportingSystem.API.Extensions
{
    /// <summary>
    /// OpenTelemetry → Azure Monitor (Application Insights).
    /// Activates only if APPLICATIONINSIGHTS_CONNECTION_STRING exists.
    /// Sampling ratio is config-driven via Telemetry:SamplingRatio (0.0–1.0).
    /// </summary>
    public static class ObservabilityExtensions
    {
        public static IServiceCollection AddAppTelemetry(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment env,
            ILoggingBuilder logging)
        {
            var connectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // Skip telemetry when no connection string (e.g., tests).
                return services;
            }

            // Read sampling from configuration; default to 10%
            var ratio = configuration.GetValue<double?>("Telemetry:SamplingRatio") ?? 0.10;
            if (ratio <= 0 || ratio > 1) ratio = 0.10;

            services.AddOpenTelemetry()
                .ConfigureResource(r =>
                {
                    // Equivalent to "cloud role" naming in AI
                    r.AddService(
                        serviceName: "incident-api",
                        serviceVersion: configuration["Api:Version"] ?? "v1",
                        serviceInstanceId: Environment.MachineName);

                    r.AddAttributes(new[]
                    {
                        new KeyValuePair<string, object>("deployment.environment", env.EnvironmentName),
                        new KeyValuePair<string, object>("project", "IncidentReportingSystem"),
                    });
                })
                .UseAzureMonitor() // reads APPLICATIONINSIGHTS_CONNECTION_STRING
                .WithTracing(b =>
                {
                    b.SetSampler(new TraceIdRatioBasedSampler(ratio));

                    b.AddAspNetCoreInstrumentation(o =>
                    {
                        o.RecordException = true;

                        o.Filter = ctx =>
                        {
                            var raw = ctx.Request.Path.Value ?? string.Empty;
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

                            // Optionally drop synthetic probes by UA
                            var ua = ctx.Request.Headers.UserAgent.ToString().ToLowerInvariant();
                            if (!string.IsNullOrEmpty(ua) &&
                                (ua.Contains("alwayson") ||
                                 ua.Contains("kube-probe") ||
                                 ua.Contains("availability") ||
                                 ua.Contains("uptime") ||
                                 ua.Contains("pingdom")))
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

            /// Open Telemtry ///
            var serviceName = "IncidentReportingSystem.API";

            services.AddOpenTelemetry()
              .ConfigureResource(res => res.AddService(serviceName))
              .WithTracing(traces =>
              {
                  traces
                      .AddAspNetCoreInstrumentation(o =>
                      {
                          o.RecordException = true;
                      })
                      .AddHttpClientInstrumentation()
                      .AddAzureMonitorTraceExporter();
              })
              .WithMetrics(metrics =>
              {
                  metrics.AddAspNetCoreInstrumentation()
                         .AddHttpClientInstrumentation()
                         .AddAzureMonitorMetricExporter();
              });

            // Structured logs to App Insights via OpenTelemetry:
            logging.ClearProviders();
            logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
                logging.AddAzureMonitorLogExporter();
            });
            return services;
        }
    }
}
