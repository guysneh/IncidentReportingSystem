using System.Net;
using System.Net.Http.Headers;

namespace IncidentReportingSystem.UI.Core.Http
{
    internal sealed class AuthHeaderHandler : DelegatingHandler
    {
        private readonly Auth.AuthState _state;
        private readonly Auth.AuthEvents _events;

        public AuthHeaderHandler(Auth.AuthState state, Auth.AuthEvents events)
        {
            _state = state;
            _events = events;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (_events.IsUnauthorizedTripped)
                throw new HttpRequestException("Unauthorized (short-circuited)");

            var token = _state.AccessToken;
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var resp = await base.SendAsync(request, ct);

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                _events.TripUnauthorized(request.RequestUri?.ToString());
            }

            return resp;
        }
    }
}
