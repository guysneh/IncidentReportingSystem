using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;

namespace IncidentReportingSystem.Application.Abstractions.Persistence
{
    /// <summary>Repository abstraction for the Attachment aggregate.</summary>
    public interface IAttachmentRepository
    {
        Task AddAsync(Attachment entity, CancellationToken cancellationToken);
        Task<Attachment?> GetAsync(Guid id, CancellationToken cancellationToken);
        Task<Attachment?> GetReadOnlyAsync(Guid id, CancellationToken cancellationToken);

        /// <summary>
        /// Returns a page of attachments for a given parent, newest-first, along with the total count.
        /// </summary>
        Task<(IReadOnlyList<Attachment> Items, int Total)> ListByParentAsync(
            AttachmentParentType parentType,
            Guid parentId,
            int skip,
            int take,
            CancellationToken cancellationToken);

        /// <summary>
        /// Removes an attachment entity from the persistence context.
        /// </summary>
        Task RemoveAsync(Attachment entity, CancellationToken cancellationToken);
    }
}
