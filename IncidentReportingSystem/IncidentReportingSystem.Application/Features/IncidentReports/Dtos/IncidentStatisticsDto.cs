namespace IncidentReportingSystem.Application.Features.IncidentReports.Dtos;

/// <summary>
/// Aggregated statistics of incident reports.
/// </summary>
public class IncidentStatisticsDto
{
    /// <summary>
    /// Number of incidents per category.
    /// </summary>
    public Dictionary<string, int> IncidentsByCategory { get; init; } = new();

    /// <summary>
    /// Number of incidents per severity level.
    /// </summary>
    public Dictionary<string, int> IncidentsBySeverity { get; init; } = new();

    /// <summary>
    /// Number of incidents by status (Open/Closed).
    /// </summary>
    public Dictionary<string, int> IncidentsByStatus { get; init; } = new();

    /// <summary>
    /// Total number of incident reports.
    /// </summary>
    public int TotalIncidents { get; init; }
}
