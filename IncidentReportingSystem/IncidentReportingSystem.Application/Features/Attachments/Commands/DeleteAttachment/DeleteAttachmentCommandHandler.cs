using IncidentReportingSystem.Application.Abstractions.Attachments;
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

        public DeleteAttachmentCommandHandler(IAttachmentsRepository repo, IAttachmentStorage blob, IUnitOfWork uow)
            => (_repo, _blob, _uow) = (repo, blob, uow);

        public async Task Handle(DeleteAttachmentCommand cmd, CancellationToken ct)
        {
            var a = await _repo.GetAsync(cmd.AttachmentId, ct)
                ?? throw new NotFoundException("Attachment not found");

            // מחיקת הבלוב בפועל
            await _blob.DeleteAsync(a.StoragePath, ct);

            // מחיקה מהדאטהבייס
            await _repo.RemoveAsync(a, ct);
            await _uow.SaveChangesAsync(ct);
        }
    }

}
