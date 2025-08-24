using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Application.Features.Attachments.Dtos;
using MediatR;

namespace IncidentReportingSystem.Application.Features.Attachments.Queries
{
    /// <summary>Retrieves attachment constraints for client consumption.</summary>
    public sealed record GetAttachmentConstraintsQueryHandler : IRequest<AttachmentConstraintsDto>;

    /// <summary>Maps <see cref="IAttachmentPolicy"/> to a DTO.</summary>
    public sealed class GetAttachmentConstraintsHandler
        : IRequestHandler<GetAttachmentConstraintsQueryHandler, AttachmentConstraintsDto>
    {
        private readonly IAttachmentPolicy _policy;
        public GetAttachmentConstraintsHandler(IAttachmentPolicy policy) => _policy = policy;

        public Task<AttachmentConstraintsDto> Handle(GetAttachmentConstraintsQueryHandler request, CancellationToken cancellationToken)
        {
            var dto = new AttachmentConstraintsDto
            {
                MaxSizeBytes = _policy.MaxSizeBytes,
                AllowedContentTypes = _policy.AllowedContentTypes.ToArray(),
                AllowedExtensions = _policy.AllowedExtensions.ToArray()
            };
            return Task.FromResult(dto);
        }
    }
}
