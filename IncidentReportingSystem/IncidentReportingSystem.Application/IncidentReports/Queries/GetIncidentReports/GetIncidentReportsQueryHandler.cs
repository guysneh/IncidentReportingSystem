using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Interfaces;
using MediatR;

namespace IncidentReportingSystem.Application.IncidentReports.Queries.GetIncidentReports
{
    /// <summary>
    /// Handles retrieval of incident reports from repository.
    /// </summary>
    public class GetIncidentReportsQueryHandler : IRequestHandler<GetIncidentReportsQuery, IReadOnlyList<IncidentReport>>
    {
        private readonly IIncidentReportRepository _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetIncidentReportsQueryHandler"/> class.
        /// </summary>
        /// <param name="repository">Incident report repository.</param>
        public GetIncidentReportsQueryHandler(IIncidentReportRepository repository)
        {
            _repository = repository;
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<IncidentReport>> Handle(GetIncidentReportsQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetAsync(
                status: request.Status,
                skip: request.Skip,
                take: request.Take,
                category: request.Category,
                severity: request.Severity,
                searchText: request.SearchText,
                reportedAfter: request.ReportedAfter,
                reportedBefore: request.ReportedBefore,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
        }
    }
}
