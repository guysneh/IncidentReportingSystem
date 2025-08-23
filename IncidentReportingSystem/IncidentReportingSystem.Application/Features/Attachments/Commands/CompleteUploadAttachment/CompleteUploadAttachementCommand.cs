using MediatR;

namespace IncidentReportingSystem.Application.Features.Attachments.Commands.CompleteUploadAttachment
{
    /// <summary>Completes an attachment upload after server-side validation.</summary>
    public sealed record CompleteUploadAttachmentCommand(Guid AttachmentId) : IRequest;
}
