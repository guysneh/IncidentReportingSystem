using System;
using System.Threading.Tasks;

namespace IncidentReportingSystem.UI.Core.Auth
{
    public sealed class AuthState
    {
        public string? AccessToken { get; private set; }
        public DateTimeOffset? ExpiresAtUtc { get; private set; }
        public bool IsHydrated { get; private set; }

        // Using Func<Task> so listeners can do async work safely
        public event Func<Task>? Changed;

        public Task SetAsync(string? token, DateTimeOffset? expUtc)
        {
            AccessToken = token;
            ExpiresAtUtc = expUtc;
            IsHydrated = true;
            return Changed?.Invoke() ?? Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            AccessToken = null;
            ExpiresAtUtc = null;
            IsHydrated = true;
            return Changed?.Invoke() ?? Task.CompletedTask;
        }
    }
}
