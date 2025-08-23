using FluentValidation;
using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Application.Common.Errors;
using IncidentReportingSystem.Application.Common.Exceptions;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Application.Features.Attachments.Commands.StartUploadAttachment
{
    /// <summary>Starts an attachment upload by creating a pending entity + upload slot.</summary>
    public sealed record StartUploadAttachmentCommand(
        AttachmentParentType ParentType,
        Guid ParentId,
        string FileName,
        string ContentType) : IRequest<StartUploadAttachmentResponse>;

    /// <summary>Response for <see cref="StartUploadAttachmentCommand"/>.</summary>
    public sealed record StartUploadAttachmentResponse(Guid AttachmentId, Uri UploadUrl, DateTimeOffset ExpiresAt);
}
