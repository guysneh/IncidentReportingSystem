using IncidentReportingSystem.Application.Features.Attachments.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Application.Features.Attachments.Queries.GetAttachmentConstraints
{
    /// <summary>Retrieves attachment constraints for client consumption.</summary>
    public sealed record GetAttachmentConstraintsQuery : IRequest<AttachmentConstraintsDto>;
}
