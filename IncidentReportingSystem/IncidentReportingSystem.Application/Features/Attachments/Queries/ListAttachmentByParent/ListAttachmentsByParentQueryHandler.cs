using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Application.Common.Models;
using IncidentReportingSystem.Application.Features.Attachments.Dtos;
using IncidentReportingSystem.Domain.Enums;
using MediatR;

namespace IncidentReportingSystem.Application.Features.Attachments.Queries.ListAttachmentsByParent
{
    /// <summary>
    /// Delegates list+count to repository and maps to DTOs.
    /// Ensures paging values (skip/take) are sanitized and reflected in the response.
    /// </summary>
    public sealed class ListAttachmentsByParentQueryHandler
        : IRequestHandler<ListAttachmentsByParentQuery, PagedResult<AttachmentDto>>
    {
        private readonly IAttachmentRepository _repo;
        private readonly ICurrentUserService _currentUserService;

        public ListAttachmentsByParentQueryHandler(IAttachmentRepository repo, ICurrentUserService currentUserService)
        {
            _repo = repo;
            _currentUserService = currentUserService;
        }

        public async Task<PagedResult<AttachmentDto>> Handle(
    ListAttachmentsByParentQuery request,
    CancellationToken ct)
        {
            // Sanitize incoming paging before delegating to the repository
            var effectiveSkip = request.Skip < 0 ? 0 : request.Skip;
            var effectiveTake = request.Take <= 0 ? 100 : request.Take; // default page size

            var (entities, total) = await _repo.ListByParentAsync(
                request.ParentType, request.ParentId, effectiveSkip, effectiveTake, ct)
                .ConfigureAwait(false);

            var userId = _currentUserService.UserIdOrThrow(); // currently unused for flags, kept for future ownership logic

            // Map entities -> DTOs (no 'page' variable; use 'entities')
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
                HasThumbnail = a.HasThumbnail,
                CanDownload = a.Status == AttachmentStatus.Completed,
                CanDelete = false
            }).ToArray();

            // Return the effective values (post-clamp) so clients/tests see the real paging contract.
            return new PagedResult<AttachmentDto>(items, total, effectiveSkip, effectiveTake);
        }

    }
}
