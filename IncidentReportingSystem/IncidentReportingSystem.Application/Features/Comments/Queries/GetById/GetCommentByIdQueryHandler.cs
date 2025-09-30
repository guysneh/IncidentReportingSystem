using IncidentReportingSystem.Application.Abstractions.Identity;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using MediatR;

namespace IncidentReportingSystem.Application.Features.Comments.Queries.GetById;

public sealed class GetCommentByIdQueryHandler : IRequestHandler<GetCommentByIdQuery, CommentDto>
{
    private readonly IIncidentCommentsRepository _repo;
    private readonly IUserDirectory _userDirectory;

    public GetCommentByIdQueryHandler(IIncidentCommentsRepository repo, IUserDirectory userDirectory)
    {
        _repo = repo;
        _userDirectory = userDirectory;
    }

    public async Task<CommentDto> Handle(GetCommentByIdQuery request, CancellationToken cancellationToken)
    {
        var c = await _repo.GetAsync(request.IncidentId, request.CommentId, cancellationToken).ConfigureAwait(false);
        if (c is null) throw new KeyNotFoundException($"Comment {request.CommentId} not found.");

        string? display = null;
        var map = await _userDirectory.ResolveDisplayNamesAsync(new[] { c.UserId }, cancellationToken)
                                      .ConfigureAwait(false);
        if (map.TryGetValue(c.UserId, out var dn) && !string.IsNullOrWhiteSpace(dn))
            display = dn;

        return new CommentDto
        {
            Id = c.Id,
            IncidentId = c.IncidentId,
            UserId = c.UserId,
            UserDisplayName = display,
            Text = c.Text ?? string.Empty,
            CreatedAtUtc = c.CreatedAtUtc
        };
    }
}
