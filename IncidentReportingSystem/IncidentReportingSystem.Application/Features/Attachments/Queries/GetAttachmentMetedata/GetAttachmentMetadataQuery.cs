using IncidentReportingSystem.Application.Features.Attachments.Dtos;
using MediatR;

namespace IncidentReportingSystem.Application.Features.Attachments.Queries.GetAttachmentMetedata
{
    /// <summary>Retrieves attachment metadata by id.</summary>
    public sealed record GetAttachmentMetadataQuery(Guid AttachmentId) : IRequest<AttachmentDto>;

}
