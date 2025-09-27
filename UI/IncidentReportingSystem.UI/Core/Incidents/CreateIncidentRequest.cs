using System;
using System.Text.Json.Serialization;

namespace IncidentReportingSystem.UI.Core.Incidents
{
    public sealed class CreateIncidentRequest
    {
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("systemAffected")]
        public string? SystemAffected { get; set; }

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = string.Empty;

        [JsonPropertyName("reportedAt")]
        public DateTime? ReportedAt { get; set; }
    }
}
