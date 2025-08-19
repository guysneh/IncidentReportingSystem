using MediatR;
using IncidentReportingSystem.Application.Comments.Commands;
using IncidentReportingSystem.Application.Comments.DTOs;
using IncidentReportingSystem.Domain.Interfaces;
using IncidentReportingSystem.Domain.Entities;

namespace IncidentReportingSystem.Application.Comments.Handlers;

public sealed class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, CommentDto>
{
    private readonly IIncidentCommentsRepository _comments;
    private readonly IIncidentReportRepository _incidents;
    private readonly IUnitOfWork _uow;

    public CreateCommentCommandHandler(
        IIncidentCommentsRepository comments,
        IIncidentReportRepository incidents,
        IUnitOfWork uow)
    {
        _comments = comments;
        _incidents = incidents;
        _uow = uow;
    }

    public async Task<CommentDto> Handle(CreateCommentCommand request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var entity = new IncidentComment
        {
            Id = Guid.NewGuid(),
            IncidentId = request.IncidentId,
            UserId = request.AuthorId,  // <-- comes from controller/JWT
            Text = request.Text,
            CreatedAtUtc = now
        };

        await _comments.AddAsync(entity, ct);

        // Keep incident's ModifiedAt in sync:
        await _incidents.TouchModifiedAtAsync(request.IncidentId, now, ct);

        await _uow.SaveChangesAsync(ct);

        return new CommentDto
        {
            Id = entity.Id,
            IncidentId = entity.IncidentId,
            UserId = entity.UserId,
            Text = entity.Text,
            CreatedAtUtc = entity.CreatedAtUtc
        };
    }
}
