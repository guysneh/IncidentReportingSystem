using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IncidentReportingSystem.UI.Core.Attachments
{
    public class AttachmentConstraints
    {
        [JsonPropertyName("maxSizeBytes")]
        public long MaxSizeBytes { get; set; }

        [JsonPropertyName("allowedContentTypes")]
        public string[] AllowedContentTypes { get; set; } = Array.Empty<string>();
    }

    public class UploadStartResponse
    {
        [JsonPropertyName("attachmentId")]
        public Guid AttachmentId { get; set; }

        // URL חתום/לופבאק אליו מעלים את ה-binary
        [JsonPropertyName("uploadUrl")]
        public string UploadUrl { get; set; } = string.Empty;

        // בד"כ "PUT" או "POST"
        [JsonPropertyName("method")]
        public string Method { get; set; } = "PUT";

        // לעתים יש כותרות נדרשות (S3/Loopback)
        [JsonPropertyName("headers")]
        public Dictionary<string, string> Headers { get; set; } = new();
    }

    public class StartUploadRequest
    {
        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("contentType")]
        public string ContentType { get; set; } = "application/octet-stream";
    }
}
