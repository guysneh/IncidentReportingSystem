using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Application.Features.Attachments.Queries.OpenAttachmentStream
{
    /// <summary>Opens a read stream for a completed attachment.</summary>
    public sealed record OpenAttachmentStreamQuery(Guid AttachmentId) : IRequest<OpenAttachmentStreamResponse>;

    /// <summary>Result of opening an attachment stream.</summary>
    public sealed record OpenAttachmentStreamResponse(Stream Stream, string ContentType, string FileName, string ETag);


}
