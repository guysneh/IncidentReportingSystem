using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Application.Common.Models;
using IncidentReportingSystem.Application.Features.Attachments.Dtos;
using MediatR;

namespace IncidentReportingSystem.Application.Features.Attachments.Queries.ListAttachmentsByParent
{
    /// <summary>
    /// Validates and sanitizes filter/sort/paging inputs, delegates to repository,
    /// and maps Attachment entities to AttachmentDto objects. The handler keeps
    /// application-layer logic free of any EF or SQL-specific concerns.
    /// </summary>
    public sealed class ListAttachmentsByParentQueryHandler
        : IRequestHandler<ListAttachmentsByParentQuery, PagedResult<AttachmentDto>>
    {
        private const int DefaultPageSize = 100;
        private const int MaxPageSize = 200;

        private readonly IAttachmentRepository _repo;
        private readonly ICurrentUserService _currentUser;

        /// <summary>
        /// Creates a new handler instance.
        /// </summary>
        public ListAttachmentsByParentQueryHandler(
            IAttachmentRepository repo,
            ICurrentUserService currentUser)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        }

        /// <inheritdoc />
        public async Task<PagedResult<AttachmentDto>> Handle(
            ListAttachmentsByParentQuery request,
            CancellationToken ct)
        {
            var f = Sanitize(request.Filters);

            // Delegate to repository with the sanitized filters
            var (entities, total) = await _repo.ListByParentAsync(
                request.ParentType, request.ParentId, f, ct).ConfigureAwait(false);

            // Current user is available for future RBAC decisions (kept for extensibility)
            _ = _currentUser.UserIdOrThrow();

            // Map to DTOs; keep RBAC flags simple (can be extended via policies later)
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
                CanDownload = a.Status == Domain.Enums.AttachmentStatus.Completed,
                CanDelete = false
            }).ToArray();

            return new PagedResult<AttachmentDto>(items, total, f.Skip, f.Take);
        }

        /// <summary>
        /// Applies defaults and whitelists to filter/sort/paging inputs.
        /// Ensures predictable, bounded query behavior.
        /// </summary>
        private static AttachmentListFilters Sanitize(AttachmentListFilters f)
        {
            var orderBy = (f.OrderBy ?? "createdAt").Trim().ToLowerInvariant();
            orderBy = orderBy is "filename" or "size" or "createdat" ? orderBy : "createdat";

            var direction = (f.Direction ?? "desc").Trim().ToLowerInvariant();
            direction = direction is "asc" ? "asc" : "desc";

            var take = f.Take <= 0 ? DefaultPageSize : Math.Min(f.Take, MaxPageSize);
            var skip = f.Skip < 0 ? 0 : f.Skip;

            return f with
            {
                OrderBy = orderBy,
                Direction = direction,
                Take = take,
                Skip = skip
            };
        }
    }
}
