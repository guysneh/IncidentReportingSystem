using System.Text.Json.Serialization;

namespace IncidentReportingSystem.UI.Core.Statistics;

/// <summary>
/// Wire DTO returned by GET /api/v1/IncidentStatistics (aggregates only).
/// Example (Swagger):
/// {
///   "incidentsByCategory": { "Network": 3, "Security": 2 },
///   "incidentsBySeverity": { "Low": 2, "High": 3 },
///   "incidentsByStatus":   { "Open": 4, "Closed": 1 },
///   "totalIncidents": 5
/// }
/// </summary>
public sealed class IncidentStatisticsDto
{
    [JsonPropertyName("incidentsByCategory")]
    public Dictionary<string, int> IncidentsByCategory { get; init; } = new();

    [JsonPropertyName("incidentsBySeverity")]
    public Dictionary<string, int> IncidentsBySeverity { get; init; } = new();

    [JsonPropertyName("incidentsByStatus")]
    public Dictionary<string, int> IncidentsByStatus { get; init; } = new();

    [JsonPropertyName("totalIncidents")]
    public int TotalIncidents { get; init; }
}
