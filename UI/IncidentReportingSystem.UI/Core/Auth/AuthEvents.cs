using System;
using System.Threading;

namespace IncidentReportingSystem.UI.Core.Auth
{
    public sealed class AuthEvents
    {
        private int _tripped;
        public bool IsUnauthorizedTripped => _tripped == 1;

        public event Action<string?>? Unauthorized;

        public void TripUnauthorized(string? source = null)
        {
            if (Interlocked.Exchange(ref _tripped, 1) == 1) return; 
            try { Unauthorized?.Invoke(source); } catch { /* ignore */ }
        }

        public void Reset() => Interlocked.Exchange(ref _tripped, 0);
    }
}
