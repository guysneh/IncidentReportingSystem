using IncidentReportingSystem.Application.Comments.Commands;
using IncidentReportingSystem.Application.Common.Exceptions;
using IncidentReportingSystem.Domain.Interfaces;
using MediatR;

namespace IncidentReportingSystem.Application.Comments.Handlers
{
    /// <summary>
    /// Handles deletion of a comment, enforcing owner-or-admin authorization
    /// using identity carried on the command.
    /// </summary>
    public sealed class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand>
    {
        private readonly IIncidentCommentsRepository _repo;
        private readonly IUnitOfWork _uow;

        public DeleteCommentCommandHandler(IIncidentCommentsRepository repo, IUnitOfWork uow)
        { _repo = repo; _uow = uow; }

        /// <inheritdoc />
        public async Task Handle(DeleteCommentCommand request, CancellationToken ct)
        {
            var entity = await _repo.GetAsync(request.IncidentId, request.CommentId, ct);
            if (entity is null)
                throw new KeyNotFoundException($"Comment {request.CommentId} not found for incident {request.IncidentId}.");

            var isOwner = entity.UserId == request.RequestedBy;
            var isAdmin = request.RequesterIsAdmin; // admin flag passed from API
            if (!isOwner && !isAdmin)
                  throw new ForbiddenException("Only the author or an Admin may delete this comment.");

            await _repo.RemoveAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);
            // No return: MediatR version expects Task (non-generic) for IRequestHandler<TRequest>.
        }
    }
}