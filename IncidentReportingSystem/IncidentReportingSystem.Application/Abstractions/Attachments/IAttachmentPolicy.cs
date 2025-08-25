using System.Collections.Generic;

namespace IncidentReportingSystem.Application.Abstractions.Attachments
{
    /// <summary>
    /// Read-only policy describing attachment constraints (size, types, SAS lifetime).
    /// Backed by configuration.
    /// </summary>
    public interface IAttachmentPolicy
    {
        long MaxSizeBytes { get; }
        ISet<string> AllowedContentTypes { get; }
        ISet<string> AllowedExtensions { get; }
        int SasMinutesToLive { get; }
    }
}
