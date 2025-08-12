using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace IncidentReportingSystem.Infrastructure.Telemetry
{
    /// <summary>
    /// Drops noisy telemetry such as 404s for "/" and "/robots*.txt" so they won't be ingested into AI.
    /// This reduces costs and keeps dashboards clean without affecting real errors.
    /// </summary>
    public sealed class IgnoreNoiseTelemetryProcessor : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor _next;

        public IgnoreNoiseTelemetryProcessor(ITelemetryProcessor next) => _next = next;

        public void Process(ITelemetry item)
        {
            if (item is RequestTelemetry req)
            {
                var path = req.Url?.AbsolutePath ?? string.Empty;
                var isNoisyPath =
                    path == "/" ||
                    path.StartsWith("/robots", StringComparison.OrdinalIgnoreCase);

                var is404 = req.ResponseCode == "404";

                if (isNoisyPath && is404)
                {
                    // Drop this telemetry item entirely.
                    return;
                }
            }

            _next.Process(item);
        }
    }
}
