using MediatR;
using System;

namespace IncidentReportingSystem.Application.Features.Attachments.Queries.OpenAttachmentStream
{
    public sealed record OpenAttachmentStreamQuery(Guid AttachmentId)
        : IRequest<OpenAttachmentStreamResponse>;
}
