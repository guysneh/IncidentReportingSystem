using System.Net.Http;
using System.Text.Json;
using IncidentReportingSystem.UI.Core.Http;

namespace IncidentReportingSystem.UI.Core.Incidents;

public sealed class IncidentsApi
{
    private readonly IApiClient _api;

    public IncidentsApi(IApiClient api) => _api = api;

    public Task<PagedResult<CommentDto>?> GetCommentsAsync(string incidentId, int skip = 0, int take = 50, CancellationToken ct = default)
        => _api.GetJsonAsync<PagedResult<CommentDto>>($"IncidentReports/{incidentId}/comments?skip={skip}&take={take}", ct);

    public Task<PagedResult<AttachmentDto>?> GetAttachmentsAsync(string incidentId, int skip = 0, int take = 100, CancellationToken ct = default)
        => _api.GetJsonAsync<PagedResult<AttachmentDto>>($"IncidentReports/{incidentId}/attachments?skip={skip}&take={take}", ct);

    public async Task<string?> GetAttachmentDownloadUrlAsync(string attachmentId, int ttlMinutes = 15, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"attachments/{attachmentId}/download-url?ttlMinutes={ttlMinutes}");
        using var resp = await _api.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var raw = (await resp.Content.ReadAsStringAsync(ct)).Trim();

        if (raw.Length >= 2 && raw.StartsWith("\"") && raw.EndsWith("\""))
        {
            try { return JsonSerializer.Deserialize<string>(raw); } catch { /* ignore */ }
        }
        return raw;
    }

}
