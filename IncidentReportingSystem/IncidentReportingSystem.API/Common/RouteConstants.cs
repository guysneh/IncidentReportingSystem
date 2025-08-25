namespace IncidentReportingSystem.API.Common
{
    /// <summary>Centralized API route constants to avoid duplication.</summary>
    public static class RouteConstants
    {
        public const string ApiV = "api/v{version:apiVersion}";
        public const string Attachments = $"{ApiV}/attachments";
        public const string Incidents = $"{ApiV}/incidentreports";
        public const string Comments = $"{ApiV}/comments";
    }
}
