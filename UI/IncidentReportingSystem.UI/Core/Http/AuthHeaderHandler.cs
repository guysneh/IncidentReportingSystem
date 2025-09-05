using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using IncidentReportingSystem.UI.Core.Auth;

namespace IncidentReportingSystem.UI.Core.Http
{
    public sealed class AuthHeaderHandler : DelegatingHandler
    {
        private readonly AuthState _state;
        private readonly IJSRuntime _js;
        private readonly ILogger<AuthHeaderHandler> _log;

        private sealed class Payload { public string? token { get; set; } public System.DateTimeOffset? expUtc { get; set; } }

        public AuthHeaderHandler(AuthState state, IJSRuntime js, ILogger<AuthHeaderHandler> log)
        { _state = state; _js = js; _log = log; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var token = _state.AccessToken;

            if (string.IsNullOrWhiteSpace(token))
            {
                try
                {
                    var p = await _js.InvokeAsync<Payload>("irsAuth.get");
                    if (!string.IsNullOrWhiteSpace(p?.token))
                    {
                        await _state.SetAsync(p!.token!);
                        token = p.token;
                    }
                }
                catch { }
            }

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                _log.LogInformation("[HTTP] {m} {u} | auth=Bearer {p}***", request.Method, request.RequestUri, token[..8]);
            }
            else
            {
                _log.LogWarning("[HTTP] {m} {u} | auth=-(no token)", request.Method, request.RequestUri);
            }

            return await base.SendAsync(request, ct);
        }
    }
}
