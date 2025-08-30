using System.Threading;
using System.Threading.Tasks;
using MediatR;
using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Common.Errors;
using IncidentReportingSystem.Application.Common.Exceptions;
using IncidentReportingSystem.Application.Features.Attachments.Dtos;

namespace IncidentReportingSystem.Application.Features.Attachments.Queries.GetAttachmentStatus
{
    /// <summary>Handler that loads the attachment and probes storage for object presence/props.</summary>
    public sealed class GetAttachmentStatusQueryHandler
        : IRequestHandler<GetAttachmentStatusQuery, AttachmentStatusDto>
    {
        private readonly IAttachmentRepository _repository;
        private readonly IAttachmentStorage _storage;

        public GetAttachmentStatusQueryHandler(IAttachmentRepository repository, IAttachmentStorage storage)
        {
            _repository = repository;
            _storage = storage;
        }

        public async Task<AttachmentStatusDto> Handle(GetAttachmentStatusQuery request, CancellationToken cancellationToken)
        {
            var a = await _repository.GetReadOnlyAsync(request.AttachmentId, cancellationToken).ConfigureAwait(false)
                ?? throw new NotFoundException(AttachmentErrors.AttachmentNotFound);

            var probe = await _storage.TryGetUploadedAsync(a.StoragePath, cancellationToken).ConfigureAwait(false);

            return new AttachmentStatusDto
            {
                Status = a.Status.ToString(),
                Size = probe?.Length,
                ExistsInStorage = probe is not null,
                ContentType = probe?.ContentType
            };
        }
    }
}
