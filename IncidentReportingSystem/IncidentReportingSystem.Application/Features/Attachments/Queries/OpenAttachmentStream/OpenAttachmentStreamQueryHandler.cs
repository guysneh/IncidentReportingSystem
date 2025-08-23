using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Application.Common.Errors;
using IncidentReportingSystem.Application.Common.Exceptions;
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

        public async Task<OpenAttachmentStreamResponse> Handle(OpenAttachmentStreamQuery request, CancellationToken ct)
        {
            var a = await _repo.GetReadOnlyAsync(request.AttachmentId, ct).ConfigureAwait(false)
                ?? throw new NotFoundException(AttachmentErrors.AttachmentNotFound);

            if (a.Size is null)
                throw new InvalidOperationException(AttachmentErrors.AttachmentNotCompleted);

            var stream = await _storage.OpenReadAsync(a.StoragePath, ct).ConfigureAwait(false);
            return new OpenAttachmentStreamResponse(stream, a.ContentType, a.FileName);
        }
    }
}
