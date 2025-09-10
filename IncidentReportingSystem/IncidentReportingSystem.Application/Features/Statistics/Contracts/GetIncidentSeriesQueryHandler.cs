using IncidentReportingSystem.Application.Abstractions;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Features.Statistics.Contracts;
using MediatR;

public sealed class GetIncidentSeriesQueryHandler
    : IRequestHandler<GetIncidentSeriesQuery, IReadOnlyList<IncidentSeriesPoint>>
{
    private readonly IIncidentReadRepository _repo;
    public GetIncidentSeriesQueryHandler(IIncidentReadRepository repo) => _repo = repo;

    public Task<IReadOnlyList<IncidentSeriesPoint>> Handle(
        GetIncidentSeriesQuery request, CancellationToken ct) =>
        _repo.GetIncidentSeriesAsync(request.From, request.To, request.Granularity, ct);
}
