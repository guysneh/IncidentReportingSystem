using IncidentReportingSystem.Domain.Interfaces;
using MediatR;

namespace IncidentReportingSystem.Application.IncidentReports.Commands.UpdateIncidentStatus
{
    /// <summary>
    /// Handles the command to update the status of an incident report.
    /// </summary>
    public class UpdateIncidentStatusCommandHandler : IRequestHandler<UpdateIncidentStatusCommand, Unit>
    {
        private readonly IIncidentReportRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateIncidentStatusCommandHandler"/> class.
        /// </summary>
        /// <param name="repository">Repository for managing incident reports.</param>
        public UpdateIncidentStatusCommandHandler(IIncidentReportRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <inheritdoc />
        public async Task<Unit> Handle(UpdateIncidentStatusCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            cancellationToken.ThrowIfCancellationRequested();

            var incident = await _repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);

            if (incident is null)
            {
                throw new KeyNotFoundException($"Incident with ID '{request.Id}' was not found.");
            }

            incident.UpdateStatus(request.NewStatus);

            await _repository.SaveAsync(incident, cancellationToken).ConfigureAwait(false);

            return Unit.Value;
        }
    }
}
