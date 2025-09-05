using IncidentReportingSystem.UI.Core.Auth;
using System.Net.Http.Headers;

public sealed class AuthHeaderHandler : DelegatingHandler
{
    private readonly AuthState _state;
    private readonly ILogger<AuthHeaderHandler> _log;
    public AuthHeaderHandler(AuthState state, ILogger<AuthHeaderHandler> log)
    {
        _state = state; _log = log;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        var hadBefore = req.Headers.Authorization is not null; 
        var tok = _state.AccessToken;

        if (!string.IsNullOrWhiteSpace(tok))
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tok);
        }

        _log.LogInformation("[HTTP] {m} {u} | hadBefore={before} stateHas={has} final={final} len={len}",
            req.Method, req.RequestUri,
            hadBefore, !string.IsNullOrWhiteSpace(tok),
            req.Headers.Authorization is not null,
            req.Headers.Authorization?.Parameter?.Length ?? 0);

        return base.SendAsync(req, ct);
    }
}
