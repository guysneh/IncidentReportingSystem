using Azure.Core;
using Azure.Identity;
using IncidentReportingSystem.API.Options;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

namespace IncidentReportingSystem.API.Extensions;

public static class ConfigurationExtensions
{
    public static void AddConfigurationAndBindOptions(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;     // ConfigurationManager (IConfiguration + IConfigurationBuilder)
        var services = builder.Services;
        var env = builder.Environment;          // <-- always trust the hosting environment

        // Use the host's content root (the API project folder), not the test runner's CWD
        configuration.SetBasePath(env.ContentRootPath);

        // Outside Docker, load JSON files including the environment-specific one (Test/Dev/Prod)
        if (!IsRunningInDocker())
        {
            configuration
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
        }

        // Environment variables come last so they can override JSON when present (works in Prod & CI)
        configuration.AddEnvironmentVariables();

        if (env.IsDevelopment())
        {
            configuration.AddUserSecrets<Program>(optional: true);
        }

        // Azure App Configuration (only if explicitly enabled and endpoint is provided)
        TryAddAzureAppConfiguration(configuration);

        services.Configure<Auth.JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<Infrastructure.Auth.PasswordHashingOptions>(configuration.GetSection("Auth:PasswordHashing"));

        // Strongly-typed app settings
        services.Configure<MyAppSettings>(configuration.GetSection("MyAppSettings"));

        // Diagnostics state for AppConfig refresh
        services.AddSingleton<ConfigRefreshState>();

        // Add provider registrations required for App Configuration / feature flags
        services.AddAzureAppConfiguration();
        services.AddFeatureManagement();

        // Hook: update refresh timestamp + log when MyAppSettings change
        services.PostConfigure<IOptionsMonitor<MyAppSettings>>(monitor =>
        {
            // no-op here; handled at runtime in the pipeline via IOptionsMonitor.OnChange
        });
    }

    private static bool IsRunningInDocker() =>
        File.Exists("/.dockerenv") || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

    private static void TryAddAzureAppConfiguration(ConfigurationManager configuration)
    {
        var enabled = configuration.GetValue<bool>("AppConfig:Enabled");
        if (!enabled) { configuration["AppConfig:__Active"] = "false"; return; }

        var endpointValue = configuration["AppConfig:Endpoint"];
        if (string.IsNullOrWhiteSpace(endpointValue) || !Uri.TryCreate(endpointValue, UriKind.Absolute, out var endpoint))
        {
            configuration["AppConfig:__Active"] = "false";
            return;
        }

        var label = configuration["AppConfig:Label"];
        var cacheSeconds = configuration.GetValue<int?>("AppConfig:CacheSeconds") ?? 90;
        const string sentinelKey = "AppConfig:Sentinel";

        try
        {
            TokenCredential cred = new ChainedTokenCredential(
                new EnvironmentCredential(),
                new ManagedIdentityCredential(),
                new AzureCliCredential()
            );

            configuration.AddAzureAppConfiguration(options =>
            {
                options.Connect(endpoint, cred)
                       .ConfigureKeyVault(kv => kv.SetCredential(cred))
                       .Select(KeyFilter.Any, string.IsNullOrWhiteSpace(label) ? null : label)
                       .ConfigureRefresh(refresh =>
                           refresh.Register(sentinelKey, refreshAll: true)
                                  .SetCacheExpiration(TimeSpan.FromSeconds(cacheSeconds))
                       )
                       .UseFeatureFlags(ff =>
                           ff.CacheExpirationInterval = TimeSpan.FromSeconds(cacheSeconds)
                       );
            });

            configuration["AppConfig:__Active"] = "true";
        }
        catch
        {
            configuration["AppConfig:__Active"] = "false";
        }
    }
}
