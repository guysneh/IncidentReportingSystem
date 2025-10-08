using System;

namespace IncidentReportingSystem.Application.Features.Attachments.Queries.OpenAttachmentStream
{
    /// <summary>Response for opening a readable attachment stream.</summary>
    public sealed class OpenAttachmentStreamResponse
    {
        public Stream Stream { get; }
        public string? ContentType { get; }
        public string? FileName { get; }
        public string? ETag { get; }
        public DateTimeOffset? LastModifiedUtc { get; }

        public OpenAttachmentStreamResponse(
            Stream stream,
            string? contentType,
            string? fileName,
            string? eTag,
            DateTimeOffset? lastModifiedUtc = null)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            ContentType = contentType;
            FileName = fileName;
            ETag = eTag;
            LastModifiedUtc = lastModifiedUtc;
        }
    }
}
