using IncidentReportingSystem.Application.Comments.DTOs;
using IncidentReportingSystem.Application.Comments.Queries;
using IncidentReportingSystem.Domain.Interfaces;
using MediatR;

namespace IncidentReportingSystem.Application.Comments.Handlers
{
    /// <summary>Returns newest-first comments with basic pagination.</summary>
    public sealed class GetCommentsQueryHandler : IRequestHandler<GetCommentsQuery, IReadOnlyList<CommentDto>>
    {
        private readonly IIncidentCommentsRepository _repo;
        public GetCommentsQueryHandler(IIncidentCommentsRepository repo) => _repo = repo;

        /// <inheritdoc />
        public async Task<IReadOnlyList<CommentDto>> Handle(GetCommentsQuery request, CancellationToken ct)
        {
            var list = await _repo.ListAsync(request.IncidentId, request.Skip, request.Take, ct);
            var result = new List<CommentDto>(list.Count);
            foreach (var c in list)
            {
                result.Add(new CommentDto
                {
                    Id = c.Id,
                    IncidentId = c.IncidentId,
                    UserId = c.UserId,
                    Text = c.Text,
                    CreatedAtUtc = c.CreatedAtUtc
                });
            }
            return result;
        }
    }
}