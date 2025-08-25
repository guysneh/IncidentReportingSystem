using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Application.Common.Errors;
using IncidentReportingSystem.Application.Common.Exceptions;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Application.Features.Attachments.Commands.StartUploadAttachment
{
    /// <summary>
    /// Handler that checks parent existence, persists a pending attachment via repository,
    /// asks storage for a slot, assigns storage path, and returns an upload URL.
    /// </summary>
    public sealed class StartUploadAttachmentCommandHandler : IRequestHandler<StartUploadAttachmentCommand, StartUploadAttachmentResponse>
    {
        private readonly IAttachmentParentReadService _parents;
        private readonly IAttachmentRepository _repo;
        private readonly IAttachmentStorage _storage;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;

        public StartUploadAttachmentCommandHandler(
            IAttachmentParentReadService parents,
            IAttachmentRepository repo,
            IAttachmentStorage storage,
            IUnitOfWork uow,
            ICurrentUserService currentUser)
        {
            _parents = parents;
            _repo = repo;
            _storage = storage;
            _uow = uow;
            _currentUser = currentUser;
        }

        public async Task<StartUploadAttachmentResponse> Handle(StartUploadAttachmentCommand request, CancellationToken cancellationToken)
        {
            var parentExists = await _parents.ExistsAsync(request.ParentType, request.ParentId, cancellationToken).ConfigureAwait(false);
            if (!parentExists)
                throw new NotFoundException(AttachmentErrors.ParentNotFound);

            var attachment = new Attachment(
                request.ParentType,
                request.ParentId,
                request.FileName,
                request.ContentType,
                initialStoragePath: "pending",
                uploadedBy: _currentUser.UserIdOrThrow());

            await _repo.AddAsync(attachment, cancellationToken).ConfigureAwait(false);
            await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var prefix = request.ParentType == AttachmentParentType.Incident
                ? $"incidents/{request.ParentId}"
                : $"comments/{request.ParentId}";

            var slot = await _storage.CreateUploadSlotAsync(
                new CreateUploadSlotRequest(attachment.Id, request.FileName, request.ContentType, prefix), cancellationToken).ConfigureAwait(false);

            attachment.AssignStoragePath(slot.StoragePath);
            await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
;
            return new StartUploadAttachmentResponse(attachment.Id, slot.UploadUrl, slot.StoragePath);
        }
    }
}
