using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IncidentReportingSystem.UI.Core.Auth
{
    public sealed class AuthState
    {
        public string? AccessToken { get; private set; }
        public DateTimeOffset? ExpiresAtUtc { get; private set; }
        public bool IsHydrated { get; private set; }

        // Async event ONLY — all handlers must be Func<Task>
        public event Func<Task>? Changed;

        // TCS to allow pages to await hydration
        private readonly TaskCompletionSource<bool> _hydratedTcs =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task HydrateAsync(string? token, DateTimeOffset? expUtc)
        {
            AccessToken = string.IsNullOrWhiteSpace(token) ? null : token;
            ExpiresAtUtc = expUtc;
            IsHydrated = true;
            _hydratedTcs.TrySetResult(true);
            await RaiseChangedAsync();
        }

        public Task SetAsync(string? token) => HydrateAsync(token, ExpiresAtUtc);
        public Task ClearAsync() => HydrateAsync(null, null);

        public Task WaitForHydrationAsync(TimeSpan? timeout = null)
        {
            if (IsHydrated) return Task.CompletedTask;
            return timeout is null
                ? _hydratedTcs.Task
                : Task.WhenAny(_hydratedTcs.Task, Task.Delay(timeout.Value));
        }

        private Task RaiseChangedAsync()
        {
            var handlers = Changed;
            if (handlers is null) return Task.CompletedTask;

            var tasks = handlers.GetInvocationList()
                                .Cast<Func<Task>>()
                                .Select(h =>
                                {
                                    try { return h(); }
                                    catch { return Task.CompletedTask; }
                                });

            return Task.WhenAll(tasks);
        }
    }
}
