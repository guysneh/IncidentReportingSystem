using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;
using IncidentReportingSystem.UI.Core.Auth;
using Microsoft.JSInterop;

public partial class App : ComponentBase, IDisposable
{
    private IDisposable? _locationReg;
    private int _redirecting; // 0 = no, 1 = yes
    private const string LoginPath = "/login"; 

    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IAuthService Auth { get; set; } = default!;
    [Inject] private ILogger<App> Logger { get; set; } = default!;
    [Inject] private AuthState State { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } 
    protected override Task OnInitializedAsync()
    {
        _locationReg = Nav.RegisterLocationChangingHandler(OnLocationChangingAsync);
        return Task.CompletedTask;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await State.EnsureHydratedAsync(JS);
        Logger.LogInformation("[HYDRATOR] hydrated={hydr}, authorized={auth}", State.IsHydrated, State.IsAuthorized);
        StateHasChanged();
    }

    private async ValueTask OnLocationChangingAsync(LocationChangingContext context)
    {
        if (System.Threading.Interlocked.CompareExchange(ref _redirecting, 1, 1) == 1)
            return;

        var relative = Nav.ToBaseRelativePath(context.TargetLocation);
        if (IsAnonymousPath(relative))
            return;

        var token = State.AccessToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            context.PreventNavigation();
            await RedirectToLoginAsync();
            return;
        }

        var me = await Auth.MeAsync(context.CancellationToken);
        if (me is null)
        {
            context.PreventNavigation();
            try { await Auth.SignOutAsync(context.CancellationToken); } catch { /* ignore */ }
            await RedirectToLoginAsync();
        }
    }

    private Task RedirectToLoginAsync()
    {
        System.Threading.Interlocked.Exchange(ref _redirecting, 1);
        Nav.NavigateTo(LoginPath, forceLoad: true);
        return Task.CompletedTask;
    }

    private static bool IsAnonymousPath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return false;
        var p = relativePath.TrimStart('/');
        return p.Equals("login", StringComparison.OrdinalIgnoreCase) ||
               p.StartsWith("_content/", StringComparison.OrdinalIgnoreCase) ||
               p.StartsWith("lib/", StringComparison.OrdinalIgnoreCase) ||
               p.StartsWith("css/", StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose() => _locationReg?.Dispose();
}
