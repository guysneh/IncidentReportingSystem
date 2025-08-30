using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Application.Common.Errors;
using IncidentReportingSystem.Application.Common.Exceptions;
using IncidentReportingSystem.Application.Features.Attachments.Dtos;
using IncidentReportingSystem.Application.Abstractions.Persistence;

namespace IncidentReportingSystem.Application.Features.Attachments.Queries.GetAttachmentMetedata
{   
    /// <summary>Handler that loads and maps attachment metadata.</summary>
    public sealed class GetAttachmentMetadataHandler : IRequestHandler<GetAttachmentMetadataQuery, AttachmentDto>
    {
        private readonly IAttachmentRepository _repo;

        public GetAttachmentMetadataHandler(IAttachmentRepository repo) => _repo = repo;

        public async Task<AttachmentDto> Handle(GetAttachmentMetadataQuery request, CancellationToken cancellationToken)
        {
            var a = await _repo.GetReadOnlyAsync(request.AttachmentId, cancellationToken).ConfigureAwait(false)
                ?? throw new NotFoundException(AttachmentErrors.AttachmentNotFound);

            return new AttachmentDto
            {
                Id = a.Id,
                ParentType = a.ParentType,
                ParentId = a.ParentId,
                FileName = a.FileName,
                ContentType = a.ContentType,
                Size = a.Size,
                Status = a.Status,
                CreatedAt = a.CreatedAt,
                CompletedAt = a.CompletedAt,
                HasThumbnail = a.HasThumbnail
            };
        }
    }
}
