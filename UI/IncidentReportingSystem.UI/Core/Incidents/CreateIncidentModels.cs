using System;
using System.Text.Json.Serialization;

namespace IncidentReportingSystem.UI.Core.Incidents
{
    public sealed class CreateIncidentResponse
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
    }
}
