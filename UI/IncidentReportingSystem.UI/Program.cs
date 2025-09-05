using IncidentReportingSystem.UI;
using IncidentReportingSystem.UI.Core.Auth;
using IncidentReportingSystem.UI.Core.Http;
using IncidentReportingSystem.UI.Core.Options;
using IncidentReportingSystem.UI.Localization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net.Http.Headers;

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
builder.Services.AddTransient<AuthHeaderHandler>();

// Problems
builder.Services.AddTransient<ProblemDetailsHandler>();

// HTTP clients
static string Slash(string u) => string.IsNullOrWhiteSpace(u) ? u : (u.EndsWith("/") ? u : u + "/");
builder.Services.AddScoped<IncidentReportingSystem.UI.Core.Http.PublicApiClient>();
builder.Services.AddScoped<IncidentReportingSystem.UI.Core.Http.SecureApiClient>();
builder.Services.AddScoped<IncidentReportingSystem.UI.Core.Http.IApiClient>(sp =>
    sp.GetRequiredService<IncidentReportingSystem.UI.Core.Http.SecureApiClient>());


builder.Services.AddHttpClient("ApiPublic", (sp, c) =>
{
    var opts = sp.GetRequiredService<IOptions<ApiOptions>>().Value;
    if (string.IsNullOrWhiteSpace(opts.BaseUrl))
        throw new InvalidOperationException("Api:BaseUrl is missing");
    c.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/"); // e.g. https://localhost:7001/api/v1/
    c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddHttpClient("Api", (sp, c) =>
{
    var opts = sp.GetRequiredService<IOptions<ApiOptions>>().Value;
    if (string.IsNullOrWhiteSpace(opts.BaseUrl))
        throw new InvalidOperationException("Api:BaseUrl is missing");
    c.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/"); // e.g. https://localhost:7001/api/v1/
    c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
})
.AddHttpMessageHandler<AuthHeaderHandler>()          
.AddHttpMessageHandler<ProblemDetailsHandler>();     

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
// --- Localization: cookie first ---
var supported = new[] { "en", "de", "he" }.Select(c => new CultureInfo(c)).ToList();
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en"),
    SupportedCultures = supported,
    SupportedUICultures = supported,
    FallBackToParentCultures = false,
    FallBackToParentUICultures = false
});

app.MapGet("/localize", (HttpContext ctx, string c, string? r) =>
{
    var cookie = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(c));
    ctx.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        cookie,
        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax, Path = "/" }
    );

    var back = string.IsNullOrWhiteSpace(r) ? "/" : r;
    if (Uri.TryCreate(back, UriKind.Absolute, out var abs)) back = abs.PathAndQuery;
    return Results.LocalRedirect(back); 
});


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
