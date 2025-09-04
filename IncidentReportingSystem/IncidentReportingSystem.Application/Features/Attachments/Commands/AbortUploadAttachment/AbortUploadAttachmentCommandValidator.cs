using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Application.Features.Attachments.Commands.AbortUploadAttachment
{
    /// <summary>Validates basic structure for <see cref="AbortUploadAttachmentCommand"/>.</summary>
    public sealed class AbortUploadAttachmentCommandValidator : AbstractValidator<AbortUploadAttachmentCommand>
    {
        public AbortUploadAttachmentCommandValidator()
        {
            RuleFor(x => x.AttachmentId).NotEmpty();
            RuleFor(x => x.RequestedBy).NotEmpty();
        }
    }
}
