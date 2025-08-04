using IncidentReportingSystem.Domain.Enums;

namespace IncidentReportingSystem.Domain.Entities
{
    /// <summary>
    /// Represents a reported incident in the system.
    /// </summary>
    public class IncidentReport
    {
        /// <summary>
        /// Gets the unique identifier for the incident.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets the textual description of the incident.
        /// </summary>
        public string Description { get; private set; } = default!;

        /// <summary>
        /// Gets the physical or logical location where the incident occurred.
        /// </summary>
        public string Location { get; private set; } = default!;

        /// <summary>
        /// Gets the identifier of the user or system that reported the incident.
        /// </summary>
        public Guid ReporterId { get; private set; }

        /// <summary>
        /// Gets the category of the incident.
        /// </summary>
        public IncidentCategory Category { get; private set; }

        /// <summary>
        /// Gets the name of the affected system or service.
        /// </summary>
        public string SystemAffected { get; private set; } = default!;

        /// <summary>
        /// Gets the severity level of the incident.
        /// </summary>
        public IncidentSeverity Severity { get; private set; }

        /// <summary>
        /// Gets the timestamp when the incident was reported.
        /// </summary>
        public DateTime? ReportedAt { get; private set; }

        /// <summary>
        /// Gets the current status of the incident.
        /// </summary>
        public IncidentStatus Status { get; private set; }

        /// <summary>
        /// Gets the creation timestamp of the incident.
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IncidentReport"/> class.
        /// </summary>
        public IncidentReport(
            string description,
            string location,
            Guid reporterId,
            IncidentCategory category,
            string systemAffected,
            IncidentSeverity severity,
            DateTime? reportedAt)
        {
            Id = Guid.NewGuid();
            Description = description;
            Location = location;
            ReporterId = reporterId;
            Category = category;
            SystemAffected = systemAffected;
            Severity = severity;
            ReportedAt = reportedAt;
            CreatedAt = DateTime.UtcNow;
            Status = IncidentStatus.Open;
        }

        /// <summary>
        /// For EF Core.
        /// </summary>
        private IncidentReport() { }
    }
}
