using IncidentReportingSystem.Application.Abstractions.Identity;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Common.Models;
using IncidentReportingSystem.Application.Features.Comments.Mappers;
using MediatR;

namespace IncidentReportingSystem.Application.Features.Comments.Queries.ListComment
{
    public sealed class ListCommentsQueryHandler : IRequestHandler<ListCommentsQuery, PagedResult<CommentDto>>
    {
        private readonly IIncidentCommentsRepository _repo;
        private readonly IUserDirectory _userDirectory;

        public ListCommentsQueryHandler(IIncidentCommentsRepository repo, IUserDirectory userDirectory)
        {
            _repo = repo;
            _userDirectory = userDirectory;
        }

        public async Task<PagedResult<CommentDto>> Handle(ListCommentsQuery request, CancellationToken cancellationToken)
        {
            if (!await _repo.IncidentExistsAsync(request.IncidentId, cancellationToken).ConfigureAwait(false))
                throw new KeyNotFoundException($"Incident {request.IncidentId} not found.");

            var page = await _repo.ListPagedAsync(request.IncidentId, request.Skip, request.Take, cancellationToken)
                                  .ConfigureAwait(false);

            var items = page.Items;
            var userIds = items.Select(x => x.UserId).Distinct().ToArray();
            var map = await _userDirectory.ResolveDisplayNamesAsync(userIds, cancellationToken).ConfigureAwait(false);

            var mapped = items.Select(x =>
            {
                map.TryGetValue(x.UserId, out var dn);
                return new CommentDto
                {
                    Id = x.Id,
                    IncidentId = x.IncidentId,
                    UserId = x.UserId,
                    UserDisplayName = string.IsNullOrWhiteSpace(dn) ? null : dn,
                    Text = x.Text ?? string.Empty,
                    CreatedAtUtc = x.CreatedAtUtc
                };
            }).ToList();

            return new PagedResult<CommentDto>(mapped, page.Total, page.Skip, page.Take);
        }
    }
}
