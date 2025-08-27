using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Application.Common.Models;
using IncidentReportingSystem.Application.Features.Attachments.Dtos;
using MediatR;

namespace IncidentReportingSystem.Application.Features.Attachments.Queries.ListAttachmentsByParent
{
    /// <summary>
    /// Delegates list+count to repository and maps to DTOs.
    /// </summary>
    public sealed class ListAttachmentsByParentQueryHandler
        : IRequestHandler<ListAttachmentsByParentQuery, PagedResult<AttachmentDto>>
    {
        private readonly IAttachmentRepository _repo;
        public ListAttachmentsByParentQueryHandler(IAttachmentRepository repo) => _repo = repo;

        public async Task<PagedResult<AttachmentDto>> Handle(
            ListAttachmentsByParentQuery request,
            CancellationToken ct)
        {
            var (entities, total) = await _repo.ListByParentAsync(
                request.ParentType, request.ParentId, request.Skip, request.Take, ct).ConfigureAwait(false);

            var items = entities.Select(a => new AttachmentDto
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
            }).ToList();

            return new PagedResult<AttachmentDto>(items, total, request.Skip, request.Take);
        }
    }
}
