using System;
using IncidentReportingSystem.Application.Abstractions.Logging;
using IncidentReportingSystem.Application.Common.Logging;
using Microsoft.Extensions.Logging;

namespace IncidentReportingSystem.Infrastructure.Logging
{
    /// <summary>
    /// Logger-backed audit implementation. Keeps EventId/tags consistent for downstream filtering.
    /// </summary>
    public sealed class AttachmentAuditService : IAttachmentAuditService
    {
        private readonly ILogger<AttachmentAuditService> _logger;

        public AttachmentAuditService(ILogger<AttachmentAuditService> logger) => _logger = logger;

        public void AttachmentCompleted(Guid attachmentId)
        {
            _logger.LogInformation(
                AuditEvents.Attachments.Complete,
                "Attachment completed {tags} {AttachmentId}",
                "attachments,complete",
                attachmentId);
        }

        public void AttachmentDownloaded(Guid attachmentId, string mode, bool notModified, string? userId)
        {
            var status = notModified ? "not-modified" : "ok";
            _logger.LogInformation(
                AuditEvents.Attachments.Download,
                "Attachment download {tags} {AttachmentId} {Mode} {Status} {UserId}",
                "attachments,download",
                attachmentId,
                mode,
                status,
                userId ?? string.Empty);
        }
    }
}
