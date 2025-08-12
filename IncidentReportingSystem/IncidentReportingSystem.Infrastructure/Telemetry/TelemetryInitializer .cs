using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace IncidentReportingSystem.Infrastructure.Telemetry
{
    /// <summary>
    /// Sets a stable Cloud Role Name on every telemetry item emitted by this process.
    /// This enables filtering and correlation per service in Application Insights
    /// (e.g., filter by cloud_RoleName == "incident-api").
    /// </summary>
    public sealed class TelemetryInitializer : ITelemetryInitializer
    {
        private readonly string _roleName;

        /// <summary>
        /// Initializes a new instance of <see cref="TelemetryInitializer"/>.
        /// </summary>
        /// <param name="roleName">Logical service name to stamp on telemetry items.</param>
        public TelemetryInitializer(string roleName)
        {
            _roleName = roleName;
        }

        /// <summary>
        /// Invoked for every telemetry item; sets Cloud.RoleName if not already set.
        /// </summary>
        public void Initialize(ITelemetry telemetry)
        {
            if (string.IsNullOrWhiteSpace(telemetry.Context?.Cloud?.RoleName))
            {
                telemetry.Context.Cloud.RoleName = _roleName;
            }
        }
    }
}
