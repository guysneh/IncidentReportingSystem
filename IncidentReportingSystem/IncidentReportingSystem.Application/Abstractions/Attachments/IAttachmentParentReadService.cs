using IncidentReportingSystem.Domain.Enums;

namespace IncidentReportingSystem.Application.Abstractions.Attachments
{
    /// <summary>Read service that validates existence of a parent entity for attachments.</summary>
    public interface IAttachmentParentReadService
    {
        Task<bool> ExistsAsync(AttachmentParentType parentType, Guid parentId, CancellationToken ct);
    }
}
