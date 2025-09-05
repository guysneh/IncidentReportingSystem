using IncidentReportingSystem.UI;
using IncidentReportingSystem.UI.Core.Auth;
using IncidentReportingSystem.UI.Core.Http;
using IncidentReportingSystem.UI.Core.Options;
using IncidentReportingSystem.UI.Localization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

// Must be set before any hosting/builder is created
AppContext.SetSwitch("Microsoft.AspNetCore.Watch.BrowserRefreshEnabled", false);

var builder = WebApplication.CreateBuilder(args);

// DataProtection (containers)
var keysDir = builder.Configuration["DataProtection:KeysDirectory"] ?? "/keys";
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysDir))
    .SetApplicationName("IncidentReportingSystem.UI");

// Antiforgery
builder.Services.AddAntiforgery(o =>
{
    o.Cookie.Name = ".irs.xsrf";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Options
builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection("Api"));

// Localization
builder.Services.AddLocalization(o => o.ResourcesPath = "Localization");
builder.Services.AddScoped<IAppTexts, AppTexts>();

// Auth
builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<IAuthService, AuthService>();

// HTTP clients
static string Slash(string u) => string.IsNullOrWhiteSpace(u) ? u : (u.EndsWith("/") ? u : u + "/");

builder.Services.AddTransient<AuthHeaderHandler>();

builder.Services.AddHttpClient("Api", (sp, http) =>
{
    var api = sp.GetRequiredService<IOptions<ApiOptions>>().Value;
    http.BaseAddress = new Uri(api.BaseUrl.EndsWith("/") ? api.BaseUrl : api.BaseUrl + "/");
    http.Timeout = TimeSpan.FromSeconds(30);
}).AddHttpMessageHandler<AuthHeaderHandler>();  
builder.Services.AddHttpClient("ApiPublic", (sp, http) =>
{
    var api = sp.GetRequiredService<IOptions<ApiOptions>>().Value;
    http.BaseAddress = new Uri(api.BaseUrl.EndsWith("/") ? api.BaseUrl : api.BaseUrl + "/");
    http.Timeout = TimeSpan.FromSeconds(30);
});

// ***** THIS IS THE IMPORTANT PART FOR GUARANTEED INTERACTIVITY *****
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor(options =>
{
    options.DetailedErrors = true;           
});


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", true);
    app.UseHsts();
}
if (app.Configuration.GetValue<bool>("EnableHttpsRedirection"))
    app.UseHttpsRedirection();

app.UseStaticFiles();

// RequestLocalization (en default; de, he)
var supported = new[] { "en", "de", "he" };
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures(supported)
    .AddSupportedUICultures(supported));

app.UseAntiforgery();

// ***** MAP THE BLAZOR HUB + HOST PAGE *****
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.MapGet("/ui/culture/{code}", (string code, HttpContext ctx) =>
{
    var culture = new System.Globalization.CultureInfo(code);
    var cookie = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture));
    ctx.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        cookie,
        new CookieOptions { IsEssential = true, Expires = DateTimeOffset.UtcNow.AddYears(1), Path = "/" });

    var referer = ctx.Request.Headers.Referer.ToString();
    return Results.Redirect(string.IsNullOrWhiteSpace(referer) ? "/" : referer);
});

app.Run();

public partial class Program { }
