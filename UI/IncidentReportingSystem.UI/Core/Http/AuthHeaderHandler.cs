using IncidentReportingSystem.UI.Core.Auth;
using IncidentReportingSystem.UI.Core.Options;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

public sealed class AuthHeaderHandler : DelegatingHandler
{
    private readonly AuthState _state;

    public AuthHeaderHandler(AuthState state)
    {
        _state = state;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = _state.AccessToken;
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return base.SendAsync(request, ct);
    }
}
