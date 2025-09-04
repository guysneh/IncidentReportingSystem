using IncidentReportingSystem.UI;
using IncidentReportingSystem.UI.Core.Http;
using IncidentReportingSystem.UI.Core.Options;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Options;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Options binding ----------
builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection("Api"));

// ---------- UI services ----------
builder.Services.AddMudServices();

// ---------- HTTP: typed ApiClient + ProblemDetails handler ----------
builder.Services.AddHttpClient<ApiClient>((sp, http) =>
{
    var api = sp.GetRequiredService<IOptions<ApiOptions>>().Value;
    if (string.IsNullOrWhiteSpace(api.BaseUrl))
        throw new InvalidOperationException("Api:BaseUrl is required (appsettings or environment).");

    http.BaseAddress = new Uri(api.BaseUrl, UriKind.Absolute);
    http.Timeout = TimeSpan.FromSeconds(30);
}).AddHttpMessageHandler(() => new ProblemDetailsHandler());

// ---------- Blazor hosting ----------
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// ---------- Middleware pipeline ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();
