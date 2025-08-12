using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace IncidentReportingSystem.Infrastructure.Telemetry
{
    /// <summary>
    /// Sets a stable Cloud Role Name on every telemetry item emitted by this process.
    /// This makes it easy to filter and correlate telemetry per service/microservice
    /// in Application Insights and Log Analytics (e.g., cloud_RoleName == "incident-api").
    /// </summary>
    public sealed class TelemetryInitializer : ITelemetryInitializer
    {
        private readonly string _roleName;

        /// <summary>
        /// Creates a new <see cref="TelemetryInitializer"/>.
        /// </summary>
        /// <param name="roleName">
        /// The logical name of this service as it should appear in telemetry (e.g., "incident-api").
        /// </param>
        public TelemetryInitializer(string roleName)
        {
            _roleName = roleName;
        }

        /// <summary>
        /// Called by the Application Insights SDK for every telemetry item.
        /// We assign the Cloud.RoleName so all items are consistently labeled.
        /// </summary>
        /// <param name="telemetry">The telemetry item being initialized.</param>
        public void Initialize(ITelemetry telemetry)
        {
            // Do not overwrite if already set by another component.
            if (string.IsNullOrWhiteSpace(telemetry.Context?.Cloud?.RoleName))
            {
                telemetry.Context.Cloud.RoleName = _roleName;
            }
        }
    }
}
