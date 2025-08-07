using System.ComponentModel.DataAnnotations;
using IncidentReportingSystem.Domain.Enums;
using IncidentReportingSystem.Domain.Entities;
using MediatR;

namespace IncidentReportingSystem.Application.IncidentReports.Commands.CreateIncidentReport
{
    /// <summary>
    /// Command to create a new incident report.
    /// </summary>
    public class CreateIncidentReportCommand : IRequest<IncidentReport>
    {
        /// <summary>
        /// Description of the incident.
        /// </summary>
        [Required]
        public string Description { get; set; }

        /// <summary>
        /// Location where the incident occurred.
        /// </summary>
        [Required]
        public string Location { get; set; }

        /// <summary>
        /// Unique identifier of the reporter.
        /// </summary>
        [Required]
        public Guid ReporterId { get; set; }

        /// <summary>
        /// Category of the incident (e.g., Electrical, Software, Mechanical).
        /// </summary>
        [Required]
        public IncidentCategory Category { get; set; }

        /// <summary>
        /// The system or component affected by the incident.
        /// </summary>
        [Required]
        public string SystemAffected { get; set; }

        /// <summary>
        /// Severity level of the incident.
        /// </summary>
        [Required]
        public IncidentSeverity Severity { get; set; }

        /// <summary>
        /// Optional timestamp when the incident was reported. If null, defaults to current time.
        /// </summary>
        public DateTime? ReportedAt { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateIncidentReportCommand"/> class.
        /// </summary>
        /// <param name="description">Incident description.</param>
        /// <param name="location">Incident location.</param>
        /// <param name="reporterId">Reporter ID.</param>
        /// <param name="category">Incident category.</param>
        /// <param name="systemAffected">System affected by the incident.</param>
        /// <param name="severity">Incident severity.</param>
        /// <param name="reportedAt">Time of report.</param>
        public CreateIncidentReportCommand(
            string description,
            string location,
            Guid reporterId,
            IncidentCategory category,
            string systemAffected,
            IncidentSeverity severity,
            DateTime? reportedAt = null)
        {
            Description = description;
            Location = location;
            ReporterId = reporterId;
            Category = category;
            SystemAffected = systemAffected;
            Severity = severity;
            ReportedAt = reportedAt ?? DateTime.UtcNow;
        }
    }
}
