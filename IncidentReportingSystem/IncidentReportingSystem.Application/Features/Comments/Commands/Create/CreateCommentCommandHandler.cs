using MediatR;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Abstractions.Identity;

namespace IncidentReportingSystem.Application.Features.Comments.Commands.Create;

public sealed class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, CommentDto>
{
    private readonly IIncidentCommentsRepository _comments;
    private readonly IIncidentReportRepository _incidents;
    private readonly IUnitOfWork _uow;
    private readonly IUserDirectory _userDirectory;

    public CreateCommentCommandHandler(
        IIncidentCommentsRepository comments,
        IIncidentReportRepository incidents,
        IUnitOfWork uow,
        IUserDirectory userDirectory)
    {
        _comments = comments;
        _incidents = incidents;
        _uow = uow;
        _userDirectory = userDirectory;
    }

    public async Task<CommentDto> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
    {
        // validate incident exists using the repository's GetByIdAsync
        var incident = await _incidents.GetByIdAsync(request.IncidentId, cancellationToken).ConfigureAwait(false);
        if (incident is null)
            throw new KeyNotFoundException($"Incident {request.IncidentId} not found.");

        var now = DateTime.UtcNow;

        var entity = new IncidentComment
        {
            Id = Guid.NewGuid(),
            IncidentId = request.IncidentId,
            UserId = request.AuthorId,           // taken from token upstream
            Text = request.Text,
            CreatedAtUtc = now
        };

        await _comments.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        await _incidents.TouchModifiedAtAsync(request.IncidentId, now, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        string? display = null;
        var map = await _userDirectory.ResolveDisplayNamesAsync(new[] { entity.UserId }, cancellationToken)
                                      .ConfigureAwait(false);
        if (map.TryGetValue(entity.UserId, out var dn) && !string.IsNullOrWhiteSpace(dn))
            display = dn;

        return new CommentDto
        {
            Id = entity.Id,
            IncidentId = entity.IncidentId,
            UserId = entity.UserId,
            UserDisplayName = display,
            Text = entity.Text ?? string.Empty,
            CreatedAtUtc = entity.CreatedAtUtc
        };
    }
}
