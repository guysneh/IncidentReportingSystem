using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Application.IncidentReports.Queries.GetIncidentReports
{
    /// <summary>
    /// Handles retrieval of incident reports from repository.
    /// </summary>
    public class GetIncidentReportsQueryHandler : IRequestHandler<GetIncidentReportsQuery, IReadOnlyList<IncidentReport>>
    {
        private readonly IIncidentReportRepository _repository;

        public GetIncidentReportsQueryHandler(IIncidentReportRepository repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyList<IncidentReport>> Handle(GetIncidentReportsQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetAsync(request.IncludeClosed, request.Skip, request.Take, cancellationToken).ConfigureAwait(false);
        }
    }
}
