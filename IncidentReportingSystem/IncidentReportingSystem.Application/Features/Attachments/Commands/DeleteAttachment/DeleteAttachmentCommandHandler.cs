using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Application.Abstractions.Identity;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Common.Exceptions;
using MediatR;

namespace IncidentReportingSystem.Application.Features.Attachments.Commands.DeleteAttachment
{
    public sealed record DeleteAttachmentCommand(Guid AttachmentId) : IRequest;

    public sealed class DeleteAttachmentCommandHandler : IRequestHandler<DeleteAttachmentCommand>
    {
        private readonly IAttachmentsRepository _repo;
        private readonly IAttachmentStorage _blob; 
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUser _currentUser;

        public DeleteAttachmentCommandHandler(IAttachmentsRepository repo, IAttachmentStorage blob, IUnitOfWork uow, ICurrentUser currentUser)
            => (_repo, _blob, _uow,_currentUser) = (repo, blob, uow, currentUser);

        public async Task Handle(DeleteAttachmentCommand cmd, CancellationToken ct)
        {
            var a = await _repo.GetAsync(cmd.AttachmentId, ct)
                ?? throw new NotFoundException("Attachment not found");

            if (a.UploadedBy.ToString() != _currentUser.UserId) 
            {
                throw new ForbiddenException("The user is not allowed to delete this attachment");
            }
            // מחיקת הבלוב בפועל
            await _blob.DeleteAsync(a.StoragePath, ct);

            // מחיקה מהדאטהבייס
            await _repo.RemoveAsync(a, ct);
            await _uow.SaveChangesAsync(ct);
        }
    }

}
