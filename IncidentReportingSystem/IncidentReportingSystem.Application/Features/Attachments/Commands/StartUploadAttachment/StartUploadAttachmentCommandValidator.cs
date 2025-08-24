using FluentValidation;
using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Application.Common.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Application.Features.Attachments.Commands.StartUploadAttachment
{

    /// <summary>Validator for <see cref="StartUploadAttachmentCommand"/> — no persistence dependency.</summary>
    public sealed class StartUploadAttachmentCommandValidator : AbstractValidator<StartUploadAttachmentCommand>
    {
        public StartUploadAttachmentCommandValidator(IAttachmentPolicy policy)
        {
            RuleFor(x => x.ContentType)
                .NotEmpty()
                .Must(ct => policy.AllowedContentTypes.Contains(ct, StringComparer.OrdinalIgnoreCase))
                .WithMessage("ContentType not allowed.");

            RuleFor(x => x.FileName)
                .NotEmpty()
                .Must(fn =>
                {
                    var ext = Path.GetExtension(fn);
                    return !string.IsNullOrWhiteSpace(ext) &&
                           policy.AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
                })
                .WithMessage("File extension not allowed.");
        }
    }

}
