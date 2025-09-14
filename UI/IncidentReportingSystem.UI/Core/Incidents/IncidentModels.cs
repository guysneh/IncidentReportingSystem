using System.Text.Json.Serialization;

namespace IncidentReportingSystem.UI.Core.Incidents;

// Generic paged envelope: { total, skip, take, items: [...] }
public sealed record PagedResult<T>(
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("skip")] int Skip,
    [property: JsonPropertyName("take")] int Take,
    [property: JsonPropertyName("items")] List<T> Items);

// Comments
public sealed record CommentDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("incidentId")] string IncidentId,
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("createdAtUtc")] DateTime CreatedAtUtc);

public sealed record AttachmentDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("parentType")] string? ParentType,
    [property: JsonPropertyName("parentId")] string? ParentId,
    [property: JsonPropertyName("fileName")] string FileName,
    [property: JsonPropertyName("contentType")] string ContentType,
    [property: JsonPropertyName("size")] long Size,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("createdAt")] DateTime CreatedAt,
    [property: JsonPropertyName("completedAt")] DateTime? CompletedAt,
    [property: JsonPropertyName("hasThumbnail")] bool HasThumbnail,
    [property: JsonPropertyName("canDelete")] bool CanDelete,
    [property: JsonPropertyName("canDownload")] bool CanDownload);

public sealed record PresignedUrlResponse(
    [property: JsonPropertyName("url")] string Url);