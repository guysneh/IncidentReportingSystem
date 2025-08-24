using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Application.Features.Attachments.Dtos;
using MediatR;

namespace IncidentReportingSystem.Application.Features.Attachments.Queries.GetAttachmentConstraints
{
    /// <summary>Maps <see cref="IAttachmentPolicy"/> to <see cref="AttachmentConstraintsDto"/>.</summary>
    public sealed class GetAttachmentConstraintsHandler
        : IRequestHandler<GetAttachmentConstraintsQuery, AttachmentConstraintsDto>
    {
        private readonly IAttachmentPolicy _policy;

        public GetAttachmentConstraintsHandler(IAttachmentPolicy policy) => _policy = policy;

        public Task<AttachmentConstraintsDto> Handle(GetAttachmentConstraintsQuery request, CancellationToken ct)
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
