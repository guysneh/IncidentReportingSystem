using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Interfaces;
using MediatR;

namespace IncidentReportingSystem.Application.IncidentReports.Commands.CreateIncidentReport
{
    /// <summary>
    /// Handles creation of a new incident report.
    /// </summary>
    public class CreateIncidentReportCommandHandler : IRequestHandler<CreateIncidentReportCommand, IncidentReport>
    {
        private readonly IIncidentReportRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateIncidentReportCommandHandler"/> class.
        /// </summary>
        /// <param name="repository">Repository for persisting incident reports.</param>
        public CreateIncidentReportCommandHandler(IIncidentReportRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Handles the command to create a new incident report.
        /// </summary>
        /// <param name="request">Command containing incident report details.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>The created <see cref="IncidentReport"/>.</returns>
        public async Task<IncidentReport> Handle(CreateIncidentReportCommand request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var incident = new IncidentReport(
                description: request.Description,
                location: request.Location,
                reporterId: request.ReporterId,
                category: request.Category,
                systemAffected: request.SystemAffected,
                severity: request.Severity,
                reportedAt: request.ReportedAt
            );

            await _repository.SaveAsync(incident, cancellationToken);
            return incident;
        }
    }
}
