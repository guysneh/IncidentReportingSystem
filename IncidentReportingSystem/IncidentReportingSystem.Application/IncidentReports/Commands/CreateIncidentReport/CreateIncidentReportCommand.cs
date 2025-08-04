using IncidentReportingSystem.Domain.Enums;
using IncidentReportingSystem.Domain.Entities;
using MediatR;

namespace IncidentReportingSystem.Application.IncidentReports.Commands.CreateIncidentReport
{
    /// <summary>
    /// Command for creating a new incident report.
    /// </summary>
    public record CreateIncidentReportCommand(
        /// <summary>
        /// Description of the incident.
        /// </summary>
        string Description,

        /// <summary>
        /// Location where the incident occurred.
        /// </summary>
        string Location,

        /// <summary>
        /// Unique identifier of the reporter.
        /// </summary>
        Guid ReporterId,

        /// <summary>
        /// Category of the incident.
        /// </summary>
        IncidentCategory Category,

        /// <summary>
        /// System or service affected by the incident.
        /// </summary>
        string SystemAffected,

        /// <summary>
        /// Severity level of the incident.
        /// </summary>
        IncidentSeverity Severity,

        /// <summary>
        /// Optional timestamp when the incident was reported.
        /// </summary>
        DateTime? ReportedAt
    ) : IRequest<IncidentReport>; 
}
