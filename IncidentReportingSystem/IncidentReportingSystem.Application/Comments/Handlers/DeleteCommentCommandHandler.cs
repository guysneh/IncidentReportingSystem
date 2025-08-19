using MediatR;
using IncidentReportingSystem.Application.Comments.Commands;
using IncidentReportingSystem.Application.Common.Exceptions;
using IncidentReportingSystem.Domain.Interfaces;

namespace IncidentReportingSystem.Application.Comments.Handlers;

public sealed class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand>
{
    private readonly IIncidentCommentsRepository _comments;
    private readonly IIncidentReportRepository _incidents;
    private readonly IUnitOfWork _uow;

    public DeleteCommentCommandHandler(
        IIncidentCommentsRepository comments,
        IIncidentReportRepository incidents,
        IUnitOfWork uow)
    {
        _comments = comments;
        _incidents = incidents;
        _uow = uow;
    }

    public async Task Handle(DeleteCommentCommand request, CancellationToken ct)
    {
        var entity = await _comments.GetAsync(request.IncidentId, request.CommentId, ct);
        if (entity is null)
            throw new KeyNotFoundException($"Comment {request.CommentId} not found for incident {request.IncidentId}.");

        var isOwner = entity.UserId == request.RequestedBy;
        var isAdmin = request.RequesterIsAdmin;
        if (!isOwner && !isAdmin)
            throw new ForbiddenException("Only the author or an Admin may delete this comment.");

        await _comments.RemoveAsync(entity, ct);

        // Touch parent incident ModifiedAt in the same transaction.
        var now = DateTime.UtcNow;
        await _incidents.TouchModifiedAtAsync(request.IncidentId, now, ct);

        await _uow.SaveChangesAsync(ct);
    }
}
