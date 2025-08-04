using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IncidentReportingSystem.Application.IncidentReports.Commands.CreateIncidentReport;

/// <summary>
/// Handles the logic for creating a new incident report.
/// </summary>
public class CreateIncidentReportCommandHandler : IRequestHandler<CreateIncidentReportCommand, Guid>
{
    private readonly IIncidentReportRepository _repository;
    private readonly ILogger<CreateIncidentReportCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateIncidentReportCommandHandler"/> class.
    /// </summary>
    /// <param name="repository">Repository for persisting incident reports.</param>
    /// <param name="logger">Logger instance for diagnostics and audit logging.</param>
    public CreateIncidentReportCommandHandler(
        IIncidentReportRepository repository,
        ILogger<CreateIncidentReportCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Handles the incoming command and creates a new incident report in the system.
    /// </summary>
    /// <param name="request">The command containing the report details.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The ID of the newly created incident report.</returns>
    public async Task<Guid> Handle(CreateIncidentReportCommand request, CancellationToken cancellationToken)
    {
        var report = new IncidentReport(
            id: Guid.NewGuid(),
            description: request.Description,
            location: request.Location,
            reporterId: request.ReporterId,
            category: request.Category,
            systemAffected: request.SystemAffected,
            severity: request.Severity
        );

        await _repository.SaveAsync(report, cancellationToken);

        _logger.LogInformation("Incident report created with ID: {IncidentReportId}", report.Id);

        return report.Id;
    }
}
