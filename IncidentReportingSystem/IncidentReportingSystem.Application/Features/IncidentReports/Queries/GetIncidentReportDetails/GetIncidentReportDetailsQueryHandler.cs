using IncidentReportingSystem.Application.Abstractions.Identity;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Features.IncidentReports.Dtos;
using MediatR;

namespace IncidentReportingSystem.Application.Features.IncidentReports.Queries.GetIncidentReportDetails;

public sealed class GetIncidentReportDetailsQueryHandler
    : IRequestHandler<GetIncidentReportDetailsQuery, IncidentReportDto>
{
    private readonly IIncidentReportRepository _repository;
    private readonly IUserDirectory _userDirectory;

    public GetIncidentReportDetailsQueryHandler(
        IIncidentReportRepository repository,
        IUserDirectory userDirectory)
    {
        _repository = repository;
        _userDirectory = userDirectory;
    }

    public async Task<IncidentReportDto> Handle(GetIncidentReportDetailsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            throw new KeyNotFoundException($"Incident with ID '{request.Id}' was not found.");

        var map = await _userDirectory.ResolveDisplayNamesAsync(new[] { entity.ReporterId }, cancellationToken)
                                      .ConfigureAwait(false);
        map.TryGetValue(entity.ReporterId, out var dn);
        return entity.ToDto(string.IsNullOrWhiteSpace(dn) ? null : dn);
    }
}
