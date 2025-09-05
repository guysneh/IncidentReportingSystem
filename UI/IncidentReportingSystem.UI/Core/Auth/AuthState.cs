using System;
using System.Threading;
using System.Threading.Tasks;

namespace IncidentReportingSystem.UI.Core.Auth
{
    public sealed class AuthState
    {
        public string? AccessToken { get; private set; }
        public DateTimeOffset? ExpiresAtUtc { get; private set; }
        public bool IsHydrated { get; private set; }

        // Async event
        public event Func<Task>? Changed;

        public Task SetAsync(string? token)
        {
            AccessToken = string.IsNullOrWhiteSpace(token) ? null : token;
            IsHydrated = true;         
            Changed?.Invoke();
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            AccessToken = null;
            IsHydrated = true;         
            Changed?.Invoke();
            return Task.CompletedTask;
        }

        public async Task HydrateAsync(string? token, DateTimeOffset? exp)
        {
            AccessToken = token;
            ExpiresAtUtc = exp;
            IsHydrated = true;               // ← important: end hydration even when token is null
            await RaiseChangedAsync();
        }

        private async Task RaiseChangedAsync()
        {
            var handlers = Changed;
            if (handlers is null) return;

            foreach (Func<Task> h in handlers.GetInvocationList())
            {
                try { await h(); } catch { /* do not break others */ }
            }
        }

        public async Task<bool> WaitForHydrationAsync(TimeSpan? timeout = null, CancellationToken ct = default)
        {
            if (IsHydrated) return true;

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            Func<Task>? handler = null;
            handler = () =>
            {
                tcs.TrySetResult(true);
                Changed -= handler;
                return Task.CompletedTask;
            };
            Changed += handler;

            Task task = tcs.Task;

            if (timeout.HasValue)
            {
                var delay = Task.Delay(timeout.Value, ct);
                var done = await Task.WhenAny(task, delay);
                return done == task && tcs.Task.IsCompleted;
            }

            await task;
            return true;
        }
    }
}
