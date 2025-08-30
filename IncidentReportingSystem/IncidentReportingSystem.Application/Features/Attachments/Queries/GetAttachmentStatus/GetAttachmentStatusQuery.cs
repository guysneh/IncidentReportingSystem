using System;
using MediatR;
using IncidentReportingSystem.Application.Features.Attachments.Dtos;

namespace IncidentReportingSystem.Application.Features.Attachments.Queries.GetAttachmentStatus
{
    /// <summary>Retrieves current status of an attachment by id.</summary>
    public sealed record GetAttachmentStatusQuery(Guid AttachmentId) : IRequest<AttachmentStatusDto>;
}
