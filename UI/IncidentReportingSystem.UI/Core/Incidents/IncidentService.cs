using System.Net.Http;
using System.Text.Json;
using IncidentReportingSystem.UI.Core.Http;

namespace IncidentReportingSystem.UI.Core.Incidents;

public interface IIncidentService
{
    Task<PagedResult<CommentDto>> GetCommentsAsync(string incidentId, int skip = 0, int take = 100, CancellationToken ct = default);
    Task<PagedResult<AttachmentDto>> GetIncidentAttachmentsAsync(string incidentId, int skip = 0, int take = 100, CancellationToken ct = default);
    Task<AttachmentDto?> GetAttachmentAsync(string attachmentId, CancellationToken ct = default);
    Task<string> GetAttachmentDownloadUrlAsync(string attachmentId, int ttlMinutes = 15, CancellationToken ct = default);
}

public sealed class IncidentService : IIncidentService
{
    private readonly IApiClient _api;
    public IncidentService(IApiClient api) => _api = api;

    public Task<PagedResult<CommentDto>> GetCommentsAsync(string incidentId, int skip = 0, int take = 100, CancellationToken ct = default)
        => _api.GetJsonAsync<PagedResult<CommentDto>>($"incidentreports/{incidentId}/comments?skip={skip}&take={take}", ct);

    public Task<PagedResult<AttachmentDto>> GetIncidentAttachmentsAsync(string incidentId, int skip = 0, int take = 100, CancellationToken ct = default)
        => _api.GetJsonAsync<PagedResult<AttachmentDto>>($"incidentreports/{incidentId}/attachments?skip={skip}&take={take}", ct);

    public Task<AttachmentDto?> GetAttachmentAsync(string attachmentId, CancellationToken ct = default)
        => _api.GetJsonAsync<AttachmentDto?>($"attachments/{attachmentId}", ct);

    public async Task<string> GetAttachmentDownloadUrlAsync(string attachmentId, int ttlMinutes = 15, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"attachments/{attachmentId}/download-url?ttlMinutes={ttlMinutes}");
        using var resp = await _api.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        var payload = await resp.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(payload))
            throw new InvalidOperationException("Empty download-url response.");

        // JSON?
        if (resp.Content.Headers.ContentType?.MediaType?.Equals("application/json", StringComparison.OrdinalIgnoreCase) == true
            || payload.TrimStart().StartsWith("{"))
        {
            var dto = JsonSerializer.Deserialize<PresignedUrlResponse>(payload);
            if (string.IsNullOrWhiteSpace(dto?.Url)) throw new InvalidOperationException("Missing url field.");
            return dto.Url!;
        }

        return payload.Trim().Trim('"');
    }
}
