using IncidentReportingSystem.Application.Abstractions.Persistence;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace IncidentReportingSystem.API.Auth
{
    public sealed class AttachmentOwnerRequirement : IAuthorizationRequirement { }

    public sealed class AttachmentOwnerHandler
        : AuthorizationHandler<AttachmentOwnerRequirement, Guid> // resource=attachmentId
    {
        private readonly IAttachmentsRepository _repo;
        public AttachmentOwnerHandler(IAttachmentsRepository repo) => _repo = repo;

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            AttachmentOwnerRequirement requirement,
            Guid attachmentId)
        {
            var userId = context.User.FindFirst("sub")?.Value
                         ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId)) return;

            var a = await _repo.GetAsync(attachmentId, CancellationToken.None);
            if (a is not null && string.Equals(a.UploadedBy.ToString(), userId, StringComparison.OrdinalIgnoreCase))
                context.Succeed(requirement);
        }
    }
}
