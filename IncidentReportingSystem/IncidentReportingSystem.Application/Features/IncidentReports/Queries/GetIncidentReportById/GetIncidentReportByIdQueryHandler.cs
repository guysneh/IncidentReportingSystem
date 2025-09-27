using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using MediatR;

namespace IncidentReportingSystem.Application.Features.IncidentReports.Queries.GetIncidentReportById
{
    /// <summary>Handles the query for retrieving an incident report by its ID.</summary>
    public sealed class GetIncidentReportByIdQueryHandler
        : IRequestHandler<GetIncidentReportByIdQuery, IncidentReport>
    {
        private readonly IIncidentReportRepository _repository;

        public GetIncidentReportByIdQueryHandler(IIncidentReportRepository repository)
        {
            _repository = repository;
        }

        public async Task<IncidentReport> Handle(GetIncidentReportByIdQuery request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var entity = await _repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
                throw new KeyNotFoundException($"Incident with ID '{request.Id}' was not found.");

            return entity;
        }
    }
}
