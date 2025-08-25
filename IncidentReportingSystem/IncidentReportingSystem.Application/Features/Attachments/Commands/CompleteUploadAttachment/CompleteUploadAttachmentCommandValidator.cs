using FluentValidation;

namespace IncidentReportingSystem.Application.Features.Attachments.Commands.CompleteUploadAttachment
{
    /// <summary>Pure validator (no persistence) — only structural rules.</summary>
    public sealed class CompleteUploadAttachmentCommandValidator : AbstractValidator<CompleteUploadAttachmentCommand>
    {
        public CompleteUploadAttachmentCommandValidator()
        {
            RuleFor(x => x.AttachmentId).NotEmpty();
        }
    }
}
