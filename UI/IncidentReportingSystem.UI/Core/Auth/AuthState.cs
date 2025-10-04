// AuthState.cs
using Microsoft.JSInterop;

public sealed class AuthState
{
    private readonly ILogger<AuthState> _log;
    private bool _hydrated;
    private string? _token;
    private DateTimeOffset _expiresAtUtc;

    public event Action? Changed;               // תאימות לקוד קיים
    public bool IsHydrated => _hydrated;        // alias
    public bool Hydrated => _hydrated;
    public bool Authorized => !string.IsNullOrWhiteSpace(_token) && DateTimeOffset.UtcNow < _expiresAtUtc;
    public string? AccessToken => Authorized ? _token : null;
    public DateTimeOffset? ExpiresAtUtc => _expiresAtUtc;

    private TaskCompletionSource<bool> _hydrationTcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public AuthState(ILogger<AuthState> log) => _log = log;

    private void RaiseChanged() => Changed?.Invoke();

    public async Task HydrateAsync(string token, DateTimeOffset expiresAtUtc)
    {
        _token = token;
        _expiresAtUtc = expiresAtUtc;
        _hydrated = true;
        _hydrationTcs.TrySetResult(true);
        _log.LogInformation("[AUTH] hydrated=true, exp={exp}", _expiresAtUtc);
        RaiseChanged();
        await Task.CompletedTask;
    }

    // תאימות לאחסון סשן ישן: אם יש טוקן + תוקף => hydrate, אחרת clear
    public async Task SetAsync(string? token, DateTimeOffset? expiresAtUtc)
    {
        if (!string.IsNullOrWhiteSpace(token) && expiresAtUtc.HasValue)
            await HydrateAsync(token!, expiresAtUtc.Value);
        else
            await ClearAsync();
    }

    public async Task EnsureHydratedAsync(IJSRuntime js)
    {
        if (_hydrated) return;
        try
        {
            var dto = await js.InvokeAsync<AuthStorageDto?>("irsAuth.get");
            if (dto is not null &&
                !string.IsNullOrWhiteSpace(dto.Token) &&
                dto.ExpiresAtUtc > DateTimeOffset.UtcNow)
            {
                _token = dto.Token;
                _expiresAtUtc = dto.ExpiresAtUtc;
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[AUTH] ensure hydration failed");
        }
        finally
        {
            _hydrated = true;
            _hydrationTcs.TrySetResult(true);
            _log.LogInformation("[AUTH] ensure hydrated; token? {has}", !string.IsNullOrWhiteSpace(_token));
            RaiseChanged();
        }
    }

    public Task WaitForHydrationAsync(CancellationToken ct = default)
    {
        if (_hydrated) return Task.CompletedTask;
        if (ct.CanBeCanceled) ct.Register(() => _hydrationTcs.TrySetCanceled(ct));
        return _hydrationTcs.Task;
    }

    public async Task ClearAsync()
    {
        _token = null;
        _expiresAtUtc = default;
        _hydrated = true; // "טעון", ללא טוקן
        _hydrationTcs.TrySetResult(true);
        RaiseChanged();
        await Task.CompletedTask;
    }

    public sealed class AuthStorageDto
    {
        public string? Token { get; set; }
        public DateTimeOffset ExpiresAtUtc { get; set; } // לא nullable
    }
}
