using IncidentReportingSystem.UI.Core.Auth;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

public sealed class AuthHeaderHandler : DelegatingHandler
{
    private readonly AuthState _state;
    private readonly ILogger<AuthHeaderHandler> _log;
    public AuthHeaderHandler(AuthState state, ILogger<AuthHeaderHandler> log)
    { _state = state; _log = log; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(_state.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _state.AccessToken);
            _log.LogDebug("AuthHeaderHandler: attached bearer (len={Len}) to {Method} {Path}",
                _state.AccessToken.Length, request.Method, request.RequestUri?.ToString());
        }
        else
        {
            _log.LogDebug("AuthHeaderHandler: no token for {Method} {Path}", request.Method, request.RequestUri);
        }
        return await base.SendAsync(request, ct);
    }
}
