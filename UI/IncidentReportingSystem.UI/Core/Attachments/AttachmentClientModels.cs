using System.Text.Json.Serialization;

namespace IncidentReportingSystem.UI.Core.Attachments;

public sealed class AttachmentConstraintsVm
{
    [JsonPropertyName("maxSizeBytes")] public long MaxSizeBytes { get; init; }
    [JsonPropertyName("allowedContentTypes")] public string[] AllowedContentTypes { get; init; } = [];
    [JsonPropertyName("allowedExtensions")] public string[] AllowedExtensions { get; init; } = [];
    [JsonPropertyName("uploadUrlTtlMinutes")] public int UploadUrlTtlMinutes { get; init; }
}

public sealed class StartUploadResponseVm
{
    [JsonPropertyName("attachmentId")] public Guid AttachmentId { get; init; }
    [JsonPropertyName("uploadUrl")] public string UploadUrl { get; init; } = null!;
    [JsonPropertyName("storagePath")] public string StoragePath { get; init; } = null!;
    [JsonPropertyName("method")] public string Method { get; init; } = "PUT";
    [JsonPropertyName("headers")] public Dictionary<string, string> Headers { get; init; } = new();
}
