using IncidentReportingSystem.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Domain.Entities
{
    /// <summary>
    /// Represents an incident reported by a user, including description, location, status, and metadata.
    /// </summary>
    public class IncidentReport
    {
        /// <summary>
        /// Unique identifier for the incident.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Description of the incident as reported by the user.
        /// </summary>
        public string Description { get; init; }

        /// <summary>
        /// Location of the incident (e.g., address, area).
        /// </summary>
        public string Location { get; init; }

        /// <summary>
        /// Timestamp of when the incident was reported.
        /// Defaults to current UTC time if not provided.
        /// </summary>
        public DateTime ReportedAt { get; init; } 

        /// <summary>
        /// Current status of the incident (Open, InProgress, Closed).
        /// </summary>
        public IncidentStatus Status { get; private set; } = IncidentStatus.Open;

        /// <summary>
        /// Identifier of the user who reported the incident.
        /// </summary>
        public string ReporterId { get; init; }

        /// <summary>
        /// Optional category of the incident (e.g., infrastructure, transportation).
        /// </summary>
        public IncidentCategory Category { get; init; }

        /// <summary>
        /// Optional affected system or service.
        /// </summary>
        public string? SystemAffected { get; init; }

        /// <summary>
        /// Optional severity level of the incident (Low, Medium, High).
        /// </summary>
        public IncidentSeverity Severity { get; init; } 

        public IncidentReport(
            Guid id,
            string description,
            string location,
            string reporterId,
            IncidentCategory category = IncidentCategory.Unknown,
            string? systemAffected = null,
            IncidentSeverity severity = IncidentSeverity.Medium,
            DateTime? reportedAt = null)
        {
            Id = id;
            Description = description;
            Location = location;
            ReporterId = reporterId;
            Category = category;
            SystemAffected = systemAffected;
            Severity = severity;
            ReportedAt = reportedAt ?? DateTime.UtcNow;
        }

        /// <summary>
        /// Marks the incident as closed.
        /// </summary>
        public void Close() => Status = IncidentStatus.Closed;

        /// <summary>
        /// Updates the incident's status.
        /// </summary>
        /// <param name="newStatus">New status to assign.</param>
        public void UpdateStatus(IncidentStatus newStatus) => Status = newStatus;
    }
}

