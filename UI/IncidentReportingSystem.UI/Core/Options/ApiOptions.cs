namespace IncidentReportingSystem.UI.Core.Options
{
    /// <summary>
    /// Strongly-typed options for the backend API configuration.
    /// <remarks>
    /// Set <c>Api:BaseUrl</c> in appsettings or environment variables, e.g.:
    /// <code>Api__BaseUrl=https://localhost:8080/api/v1/</code>
    /// </remarks>
    /// </summary>
    public sealed class ApiOptions
    {
        /// <summary>
        /// Absolute base URL for the backend API, including the version segment.
        /// Example: https://localhost:8080/api/v1/
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;
    }
}
