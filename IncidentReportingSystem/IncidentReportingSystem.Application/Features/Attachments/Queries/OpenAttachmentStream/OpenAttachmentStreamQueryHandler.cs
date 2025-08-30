using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Common.Errors;
using IncidentReportingSystem.Application.Common.Exceptions;
using IncidentReportingSystem.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Application.Features.Attachments.Queries.OpenAttachmentStream
{
    /// <summary>Handler that validates state and returns a readable stream from storage.</summary>
    public sealed class OpenAttachmentStreamQueryHandler : IRequestHandler<OpenAttachmentStreamQuery, OpenAttachmentStreamResponse>
    {
        private readonly IAttachmentRepository _repo;
        private readonly IAttachmentStorage _storage;

        public OpenAttachmentStreamQueryHandler(IAttachmentRepository repo, IAttachmentStorage storage)
        {
            _repo = repo; _storage = storage;
        }

        public async Task<OpenAttachmentStreamResponse> Handle(OpenAttachmentStreamQuery request, CancellationToken cancellationToken)
        {
            var a = await _repo.GetReadOnlyAsync(request.AttachmentId, cancellationToken).ConfigureAwait(false)
                ?? throw new NotFoundException(AttachmentErrors.AttachmentNotFound);

            if (a.Status != AttachmentStatus.Completed)
                throw new InvalidOperationException(AttachmentErrors.AttachmentNotCompleted);

            var props = await _storage.TryGetUploadedAsync(a.StoragePath, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException(AttachmentErrors.UploadedObjectMissing);

            var stream = await _storage.OpenReadAsync(a.StoragePath, cancellationToken).ConfigureAwait(false);
            return new OpenAttachmentStreamResponse(stream, a.ContentType, a.FileName, props.ETag);
        }
    }
}
