namespace IncidentReportingSystem.UI.Core.Dashboard;

public enum TimeResolution { Daily, Weekly }

public sealed record DashboardQuery(
    DateOnly? From = null,
    DateOnly? To = null,
    TimeResolution Resolution = TimeResolution.Daily
);
