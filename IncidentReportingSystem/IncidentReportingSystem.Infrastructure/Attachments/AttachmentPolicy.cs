using System.Collections.Generic;
using IncidentReportingSystem.Application.Abstractions.Attachments;
using Microsoft.Extensions.Options;

namespace IncidentReportingSystem.Infrastructure.Attachments
{
    /// <summary>Policy implementation backed by <see cref="AttachmentOptions"/>.</summary>
    public sealed class AttachmentPolicy : IAttachmentPolicy
    {
        private readonly AttachmentOptions _options;
        public AttachmentPolicy(IOptions<AttachmentOptions> options) => _options = options.Value;

        public long MaxSizeBytes => _options.MaxSizeBytes;
        public ISet<string> AllowedContentTypes => new HashSet<string>(_options.AllowedContentTypes);
        public ISet<string> AllowedExtensions => new HashSet<string>(_options.AllowedExtensions);
        public int SasMinutesToLive => _options.SasMinutesToLive;
    }
}
