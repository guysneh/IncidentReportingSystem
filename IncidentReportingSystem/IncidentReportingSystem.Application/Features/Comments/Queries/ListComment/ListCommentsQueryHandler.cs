using MediatR;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Domain.Entities;

namespace IncidentReportingSystem.Application.Features.Comments.Queries.ListComment;

public sealed class ListCommentsQueryHandler : IRequestHandler<ListCommentsQuery, IReadOnlyList<IncidentComment>>
{
    private readonly IIncidentCommentsRepository _repo;

    public ListCommentsQueryHandler(IIncidentCommentsRepository repo)
        => _repo = repo;

    public async Task<IReadOnlyList<IncidentComment>> Handle(ListCommentsQuery request, CancellationToken cancellationToken)
    {
        // Explicit 404 when the incident doesn't exist
        if (!await _repo.IncidentExistsAsync(request.IncidentId, cancellationToken).ConfigureAwait(false))
            throw new KeyNotFoundException($"Incident {request.IncidentId} not found.");

        return await _repo.ListAsync(request.IncidentId, request.Skip, request.Take, cancellationToken).ConfigureAwait(false);
    }
}

