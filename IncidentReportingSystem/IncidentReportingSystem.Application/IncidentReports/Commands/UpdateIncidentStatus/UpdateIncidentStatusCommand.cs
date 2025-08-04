using IncidentReportingSystem.Domain.Enums;
using MediatR;

namespace IncidentReportingSystem.Application.IncidentReports.Commands.UpdateIncidentStatus
{
    /// <summary>
    /// Command for updating the status of an incident report.
    /// </summary>
    public record UpdateIncidentStatusCommand(
        /// <summary>
        /// ID of the incident to update.
        /// </summary>
        Guid Id,

        /// <summary>
        /// New status to assign to the incident.
        /// </summary>
        IncidentStatus NewStatus
    ) : IRequest<Unit>; 
}
