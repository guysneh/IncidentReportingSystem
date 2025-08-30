using System;
using System.Collections.Generic;
using MediatR;
using IncidentReportingSystem.Domain.Enums;

namespace IncidentReportingSystem.Application.Features.Attachments.Commands.StartUploadAttachment
{
    /// <summary>Starts an attachment upload by creating a pending entity + upload slot.</summary>
    public sealed record StartUploadAttachmentCommand(
        AttachmentParentType ParentType,
        Guid ParentId,
        string FileName,
        string ContentType) : IRequest<StartUploadAttachmentResponse>;

    /// <summary>
    /// Client-facing response describing where and how to upload the content.
    /// Non-breaking extension: existing fields preserved; new 'Method' and 'Headers' added.
    /// </summary>
    public sealed record StartUploadAttachmentResponse(
        Guid AttachmentId,
        Uri UploadUrl,
        string StoragePath,
        string Method,
        IReadOnlyDictionary<string, string> Headers
    );
}
