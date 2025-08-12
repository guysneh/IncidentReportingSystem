using Microsoft.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.Extensibility;

namespace IncidentReportingSystem.Infrastructure.Telemetry
{
    /// <summary>
    /// DI helpers for telemetry.
    /// Note: Application Insights ASP.NET registration is done in the API project.
    /// </summary>
    public static class TelemetryServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a TelemetryInitializer that stamps a fixed cloud role name.
        /// </summary>
        public static IServiceCollection AddCloudRoleName(this IServiceCollection services, string roleName)
        {
            services.AddSingleton<ITelemetryInitializer>(_ => new TelemetryInitializer(roleName));
            return services;
        }
    }
}
