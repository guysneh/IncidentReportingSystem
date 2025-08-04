using IncidentReportingSystem.Domain.Enums;
using MediatR;

namespace IncidentReportingSystem.Application.IncidentReports.Commands.CreateIncidentReport
{
    /// <summary>
    /// Command representing a request to create a new incident report.
    /// </summary>
    /// <param name="Description">Detailed description of the incident.</param>
    /// <param name="Location">Location where the incident occurred (e.g., address, area).</param>
    /// <param name="ReporterId">Identifier of the user submitting the report.</param>
    /// <param name="Category">Category of the incident (e.g., infrastructure, safety).</param>
    /// <param name="SystemAffected">Name of the system or service affected, if known.</param>
    /// <param name="Severity">Severity level of the incident (Low, Medium, High).</param>
    public record CreateIncidentReportCommand(
        string Description,
        string Location,
        string ReporterId,
        IncidentCategory Category,
        string? SystemAffected,
        IncidentSeverity Severity
    ) : IRequest<Guid>;
}
