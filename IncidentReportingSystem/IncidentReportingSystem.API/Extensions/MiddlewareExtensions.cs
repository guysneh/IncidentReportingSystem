using Asp.Versioning.ApiExplorer;
using IncidentReportingSystem.API.Middleware;
using IncidentReportingSystem.API.Options;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Options;

namespace IncidentReportingSystem.API.Extensions;

public static class MiddlewareExtensions
{
    public static void UseAppPipeline(this WebApplication app)
    {
        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        app.UseMiddleware<CorrelationIdMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();
        app.UseRouting();

        // Enable App Configuration only if provider was wired successfully
        if (app.Configuration.GetValue<bool>("AppConfig:__Active"))
        {
            app.UseAzureAppConfiguration();
        }

        app.UseCors("Default");
        app.UseRateLimiter();

        // Swagger gating by feature flag (EnableSwaggerUI) when AppConfig is active
        var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        var isAppConfigActive = app.Configuration.GetValue<bool>("AppConfig:__Active");

        app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase), branch =>
        {
            if (!isAppConfigActive) return;

            branch.Use(async (ctx, next) =>
            {
                var fm = ctx.RequestServices.GetRequiredService<Microsoft.FeatureManagement.IFeatureManagerSnapshot>();
                var enabled = await fm.IsEnabledAsync("EnableSwaggerUI");
                if (!enabled)
                {
                    ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                    await ctx.Response.WriteAsync("Not Found");
                    return;
                }
                await next();
            });
        });

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                                        description.GroupName.ToUpperInvariant());
            }
        });

        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        // Options change hook (AppConfig refresh diagnostics)
        var state = app.Services.GetRequiredService<ConfigRefreshState>();
        var monitor = app.Services.GetRequiredService<IOptionsMonitor<MyAppSettings>>();
        monitor.OnChange(v =>
        {
            state.LastRefreshUtc = DateTimeOffset.UtcNow;
            app.Logger.LogInformation("App Configuration refresh detected. SampleRatio={SampleRatio}", v.SampleRatio);
        });

        // Diagnostics & utility endpoints (hidden from Swagger)
        MapDiagnostics(app);

        // API demo endpoint per version (kept as-is)
        MapVersionedDemo(app);
    }

    private static void MapDiagnostics(WebApplication app)
    {
        app.MapGet("/diagnostics/config", (IConfiguration cfg) =>
        {
            var version = cfg["AppConfig:Sentinel"] ?? "(missing)";
            var label = cfg["AppConfig:Label"] ?? "(missing)";
            var enabled = cfg["AppConfig:Enabled"] ?? "(missing)";
            var cache = cfg["AppConfig:CacheSeconds"] ?? "(missing)";
            var sampleRatio = cfg["MyAppSettings:SampleRatio"] ?? "(missing)";
            return Results.Ok(new { AppConfigEnabled = enabled, Label = label, Sentinel = version, CacheSeconds = cache, SampleRatio = sampleRatio });
        }).AllowAnonymous().ExcludeFromDescription();

        app.MapGet("/diagnostics/config/refresh-state", (ConfigRefreshState s) =>
            Results.Ok(new { s.LastRefreshUtc })
        ).AllowAnonymous().ExcludeFromDescription();

        app.MapPost("/diagnostics/config/force-refresh",
            async (IConfigurationRefresherProvider refresherProvider) =>
            {
                foreach (var r in refresherProvider.Refreshers)
                    await r.TryRefreshAsync();
                return Results.Ok(new { forced = true, atUtc = DateTimeOffset.UtcNow });
            }).AllowAnonymous().ExcludeFromDescription();

        // Liveness
        app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }))
           .AllowAnonymous()
           .ExcludeFromDescription();

        // Readiness
        app.MapHealthChecks("/health")
           .AllowAnonymous()
           .ExcludeFromDescription();

        // CORS preflight catch-all
        app.MapMethods("{*path}", new[] { "OPTIONS" }, () => Results.NoContent())
           .RequireCors("Default")
           .ExcludeFromDescription();

        // Redirect root to Swagger
        app.MapGet("/", () => Results.Redirect("/swagger"))
           .AllowAnonymous()
           .ExcludeFromDescription();

        // Robots
        app.MapGet("/robots.txt", () => Results.Text("User-agent: *\nDisallow: /", "text/plain"))
           .AllowAnonymous()
           .ExcludeFromDescription();

        app.MapGet("/robots933456.txt", () => Results.Text("OK", "text/plain"))
           .AllowAnonymous()
           .ExcludeFromDescription();
    }

    private static void MapVersionedDemo(WebApplication app)
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        var apiBase = app.Configuration.GetValue<string>("Api:BasePath") ?? "/api";

        foreach (var desc in provider.ApiVersionDescriptions)
        {
            var group = app.MapGroup($"{apiBase}/{desc.GroupName}");

            group.MapGet("config-demo", (IConfiguration cfg) =>
            {
                var authMode = (cfg["Demo:ProbeAuthMode"] ?? "Admin").Trim();

                return Results.Ok(new
                {
                    Enabled = true,
                    AppName = cfg["App:Name"],
                    ApiVersion = cfg["Api:Version"],
                    AuthMode = authMode
                });
            })
            .AllowAnonymous()
            .WithTags("Demo")
            .WithOpenApi(op =>
            {
                op.Summary = "Configuration probe (demo)";
                op.Description = "Shows live values from Azure App Configuration (key-values refreshed via sentinel).";
                return op;
            });
        }
    }
}
