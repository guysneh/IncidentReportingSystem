using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IncidentReportingSystem.Application.Features.Attachments.Queries.ListAttachmentsByParent;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;

namespace IncidentReportingSystem.Application.Abstractions.Persistence
{
    /// <summary>
    /// Abstraction for querying and mutating Attachment aggregates.
    /// </summary>
    public interface IAttachmentRepository
    {
        /// <summary>Adds the given attachment entity to the persistence context.</summary>
        Task AddAsync(Attachment entity, CancellationToken cancellationToken);

        /// <summary>Gets an attachment by id (tracked).</summary>
        Task<Attachment?> GetAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>Gets an attachment by id (read-only, not tracked).</summary>
        Task<Attachment?> GetReadOnlyAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>
        /// Lists attachments for a specific parent (Incident or Comment) using the given
        /// search/filter/sort/paging parameters, returning both the items and the total count.
        /// </summary>
        Task<(IReadOnlyList<Attachment> Items, int Total)> ListByParentAsync(
            AttachmentParentType parentType,
            Guid parentId,
            AttachmentListFilters filters,
            CancellationToken cancellationToken);

        /// <summary>Removes the given attachment entity from the context.</summary>
        Task RemoveAsync(Attachment entity, CancellationToken cancellationToken);
    }
}
