namespace IncidentReportingSystem.UI.Core.Http
{
    public interface IApiClient
    {
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct = default);

        Task<T?> GetJsonAsync<T>(string path, CancellationToken ct = default);
        Task<TRes?> PostJsonAsync<TReq, TRes>(string path, TReq body, CancellationToken ct = default);
        Task PostJsonAsync<TReq>(string path, TReq body, CancellationToken ct = default);
        Task PatchJsonAsync<TReq>(string path, TReq body, CancellationToken ct = default);
    }
}
