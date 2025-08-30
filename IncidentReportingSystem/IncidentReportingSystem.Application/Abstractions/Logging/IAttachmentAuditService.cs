using System;

namespace IncidentReportingSystem.Application.Abstractions.Logging
{
    /// <summary>
    /// Emits audit records for the attachment lifecycle, abstracting logging concerns away
    /// from controllers and handlers.
    /// </summary>
    public interface IAttachmentAuditService
    {
        /// <summary>Emits an audit record indicating the attachment was completed.</summary>
        void AttachmentCompleted(Guid attachmentId);

        /// <summary>
        /// Emits an audit record for a download attempt.
        /// </summary>
        /// <param name="attachmentId">Target attachment id.</param>
        /// <param name="mode">"auth" (authenticated) or "signed" (temporary signed URL).</param>
        /// <param name="notModified">True if response was 304 (If-None-Match), otherwise false.</param>
        /// <param name="userId">Authenticated user id when available; null for signed/anonymous.</param>
        void AttachmentDownloaded(Guid attachmentId, string mode, bool notModified, string? userId);
    }
}
