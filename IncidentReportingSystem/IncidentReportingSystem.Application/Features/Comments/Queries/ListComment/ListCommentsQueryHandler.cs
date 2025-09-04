using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Common.Models;              
using IncidentReportingSystem.Application.Features.Comments.Dtos;    
using IncidentReportingSystem.Application.Features.Comments.Mappers;  
using MediatR;

namespace IncidentReportingSystem.Application.Features.Comments.Queries.ListComment
{
    public sealed class ListCommentsQueryHandler : IRequestHandler<ListCommentsQuery, PagedResult<CommentDto>>
    {
        private readonly IIncidentCommentsRepository _repo;

        public ListCommentsQueryHandler(IIncidentCommentsRepository repo) => _repo = repo;

        public async Task<PagedResult<CommentDto>> Handle(ListCommentsQuery request, CancellationToken cancellationToken)
        {
            if (!await _repo.IncidentExistsAsync(request.IncidentId, cancellationToken).ConfigureAwait(false))
                throw new KeyNotFoundException($"Incident {request.IncidentId} not found.");

            var page = await _repo.ListPagedAsync(request.IncidentId, request.Skip, request.Take, cancellationToken)
                                  .ConfigureAwait(false);

            var mapped = page.Items.Select(x => x.ToDto()).ToList();
            return new PagedResult<CommentDto>(mapped, page.Total, page.Skip, page.Take);
        }
    }
}
