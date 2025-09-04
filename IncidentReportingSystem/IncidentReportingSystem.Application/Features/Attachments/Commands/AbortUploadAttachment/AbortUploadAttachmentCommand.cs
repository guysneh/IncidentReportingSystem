using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Application.Features.Attachments.Commands.AbortUploadAttachment
{
    /// <summary>
    /// Command to abort a pending attachment upload. Only the original uploader or an Admin may abort.
    /// If the attachment is pending, the storage object (if any) is deleted and the record is removed.
    /// </summary>
    public sealed record AbortUploadAttachmentCommand(Guid AttachmentId, Guid RequestedBy, bool RequesterIsAdmin) : IRequest;
}
