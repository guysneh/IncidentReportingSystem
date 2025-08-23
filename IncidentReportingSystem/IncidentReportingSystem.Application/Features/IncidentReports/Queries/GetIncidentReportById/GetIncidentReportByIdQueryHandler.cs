using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using MediatR;

namespace IncidentReportingSystem.Application.Features.IncidentReports.Queries.GetIncidentReportById
{
    /// <summary>
    /// Handles the query for retrieving an incident report by its ID.
    /// </summary>
    public class GetIncidentReportByIdQueryHandler : IRequestHandler<GetIncidentReportByIdQuery, IncidentReport>
    {
        private readonly IIncidentReportRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetIncidentReportByIdQueryHandler"/> class.
        /// </summary>
        /// <param name="repository">The repository for incident reports.</param>
        public GetIncidentReportByIdQueryHandler(IIncidentReportRepository repository)
        {
            _repository = repository;
        }

        /// <inheritdoc />
        public async Task<IncidentReport> Handle(GetIncidentReportByIdQuery request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            var report = await _repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);

            if (report is null)
                throw new KeyNotFoundException($"Incident with ID '{request.Id}' was not found.");

            return report;
        }
    }
}
