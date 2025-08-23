using IncidentReportingSystem.Domain.Entities;

namespace IncidentReportingSystem.Application.Abstractions.Attachments
{
    /// <summary>Repository abstraction for the Attachment aggregate.</summary>
    public interface IAttachmentRepository
    {
        Task AddAsync(Attachment entity, CancellationToken ct);
        Task<Attachment?> GetAsync(Guid id, CancellationToken ct);
        Task<Attachment?> GetReadOnlyAsync(Guid id, CancellationToken ct);
    }
}
