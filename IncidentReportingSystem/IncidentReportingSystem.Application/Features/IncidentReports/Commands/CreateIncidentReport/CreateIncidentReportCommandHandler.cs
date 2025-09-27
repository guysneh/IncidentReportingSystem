using IncidentReportingSystem.Application.Abstractions.Identity;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Application.Common.Identity;
using IncidentReportingSystem.Domain.Entities;
using MediatR;

namespace IncidentReportingSystem.Application.Features.IncidentReports.Commands.CreateIncidentReport
{
    /// <summary>
    /// Handles the creation of a new incident report.
    /// </summary>
    public class CreateIncidentReportCommandHandler : IRequestHandler<CreateIncidentReportCommand, IncidentReport>
    {
        private readonly IIncidentReportRepository _repository;
        private readonly ICurrentUser _currentUser;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateIncidentReportCommandHandler"/> class.
        /// </summary>
        /// <param name="repository">Repository for persisting incident reports.</param>
        public CreateIncidentReportCommandHandler(IIncidentReportRepository repository, ICurrentUser currentUser)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _currentUser = currentUser;
        }

        /// <inheritdoc />
        public async Task<IncidentReport> Handle(CreateIncidentReportCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            cancellationToken.ThrowIfCancellationRequested();
            var reporterId = _currentUser.UserId ?? throw new UnauthorizedAccessException("Missing user id claim.");
            var displayName = DisplayNameFormatter.Build(_currentUser.FirstName, _currentUser.LastName, _currentUser.Email);

            var incident = new IncidentReport(
                description: request.Description,
                location: request.Location,
                reporterId: Guid.Parse(reporterId),
                category: request.Category,
                systemAffected: request.SystemAffected,
                severity: request.Severity,
                reportedAt: request.ReportedAt
            );
            incident.SetReporterDisplayName(displayName);
            await _repository.SaveAsync(incident, cancellationToken).ConfigureAwait(false);

            return incident;
        }
    }
}
