using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Azure.Core;
using Azure.Identity;
using FluentValidation;
using IncidentReportingSystem.API.Authentication;
using IncidentReportingSystem.API.Converters;
using IncidentReportingSystem.API.Extensions;
using IncidentReportingSystem.API.Middleware;
using IncidentReportingSystem.API.Swagger;
using IncidentReportingSystem.Application;
using IncidentReportingSystem.Domain.Enums;
using IncidentReportingSystem.Infrastructure.Auth;
using IncidentReportingSystem.Infrastructure.Authentication;
using IncidentReportingSystem.Infrastructure.Persistence;
using IncidentReportingSystem.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.FeatureManagement;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using IncidentReportingSystem.Application.Behaviors;
using IncidentReportingSystem.Infrastructure.Persistence.Idempotency;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Application.Common.Auth;
using IncidentReportingSystem.Domain;

var builder = WebApplication.CreateBuilder(args);

// 1) Configuration (first)
ConfigureConfiguration(builder.Configuration, builder.Services);

// 2) Services & DI
ConfigureServices(builder);

// Telemetry (OpenTelemetry + Azure Monitor)
builder.Services.AddAppTelemetry(builder.Configuration, builder.Environment);

// 3) Build
var app = builder.Build();

// 4) Middleware pipeline
ConfigureMiddleware(app);

app.Run();


// =======================
// Helpers (local methods)
// =======================
static void ConfigureConfiguration(ConfigurationManager configuration, IServiceCollection services)
{
    var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
    configuration.SetBasePath(Directory.GetCurrentDirectory());

    if (!IsRunningInDocker())
    {
        configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        configuration.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);
    }

    configuration.AddEnvironmentVariables();

    // Azure App Configuration (only if explicitly enabled and endpoint is provided)
    TryAddAzureAppConfiguration(configuration);

    services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
    services.Configure<PasswordHashingOptions>(configuration.GetSection("Auth:PasswordHashing"));

    // *** ADDED: safe strongly-typed app settings (works with or without AppConfig)
    services.Configure<MyAppSettings>(configuration.GetSection("MyAppSettings"));
}

static bool IsRunningInDocker() =>
    File.Exists("/.dockerenv") || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

static void ConfigureServices(WebApplicationBuilder builder)
{
    var services = builder.Services;
    var configuration = builder.Configuration;
    services.AddAzureAppConfiguration();
    services.AddFeatureManagement();

    // *** ADDED: refresh state for diagnostics
    services.AddSingleton<ConfigRefreshState>();

    services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
            RateLimitPartition.GetFixedWindowLimiter("default", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromSeconds(10),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5,
                AutoReplenishment = true
            }));
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    services.AddHealthChecks()
        .AddNpgSql(configuration["ConnectionStrings:DefaultConnection"]);

    services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new EnumConverterFactory());
        })
        .ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .ToDictionary(
                        e => e.Key,
                        e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray()
                    );

                var result = new
                {
                    error = "Validation failed",
                    details = errors
                };

                return new BadRequestObjectResult(result);
            };
        });

    var origins = GetAllowedOrigins(configuration);

    services.AddCors(options =>
    {
        options.AddPolicy("Default", b =>
            b.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
             .AllowAnyHeader()
             .AllowAnyMethod()
             .AllowCredentials());
    });

    services.AddApiVersioning(options =>
    {
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.ReportApiVersions = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    services.AddEndpointsApiExplorer();
    services.ConfigureOptions<ConfigureSwaggerOptions>();
    services.AddSwaggerGen(c =>
    {
        c.SupportNonNullableReferenceTypes();
        c.UseInlineDefinitionsForEnums();

        c.MapType<IncidentSeverity>(() => new OpenApiSchema
        {
            Type = "string",
            Enum = Enum.GetNames(typeof(IncidentSeverity))
                       .Select(v => (IOpenApiAny)new OpenApiString(v))
                       .ToList()
        });

        c.MapType<IncidentCategory>(() => new OpenApiSchema
        {
            Type = "string",
            Enum = Enum.GetNames(typeof(IncidentCategory))
                       .Select(v => (IOpenApiAny)new OpenApiString(v))
                       .ToList()
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "Enter **only** your JWT token. Do NOT include the word 'Bearer'.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyReference).Assembly));
    services.AddValidatorsFromAssembly(typeof(ApplicationAssemblyReference).Assembly);
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

    var connectionString = configuration["ConnectionStrings:DefaultConnection"];
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("No valid database connection string found.");

    services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
    services.AddScoped<IIncidentReportRepository, IncidentReportRepository>();
    services.AddScoped<IIncidentCommentsRepository, IncidentCommentsRepository>();
    services.AddScoped<IJwtTokenService, JwtTokenService>();
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IUnitOfWork, UnitOfWork>();
    services.AddScoped<IPasswordHasher, PasswordHasherPBKDF2>();
    services.AddScoped<IIdempotencyStore, IdempotencyStore>();

    ConfigureJwtAuthentication(services, configuration);
}

static string[] GetAllowedOrigins(IConfiguration config) =>
    (config["Cors:AllowedOrigins"] ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

static void ConfigureJwtAuthentication(IServiceCollection services, IConfiguration configuration)
{
    var jwtSecret = configuration["Jwt:Secret"];
    if (string.IsNullOrWhiteSpace(jwtSecret))
        throw new InvalidOperationException("Missing Jwt:Secret in configuration.");

    services
        .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var jwtSettings = configuration.GetSection("Jwt");

            options.MapInboundClaims = false;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(5),
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret missing"))
                ),
                RoleClaimType = ClaimTypesConst.Role,
                NameClaimType = ClaimTypesConst.Name
            };
        });

    services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

        options.AddPolicy(PolicyNames.CanReadIncidents,
            p => p.RequireRole(Roles.User, Roles.Admin));

        options.AddPolicy(PolicyNames.CanCreateIncident,
            p => p.RequireRole(Roles.User, Roles.Admin));

        options.AddPolicy(PolicyNames.CanManageIncidents,
            p => p.RequireRole(Roles.Admin));

        options.AddPolicy(PolicyNames.CanCommentOnIncident,
            p => p.RequireRole(Roles.User, Roles.Admin));

        options.AddPolicy(PolicyNames.CanDeleteComment,
            p => p.RequireRole(Roles.User, Roles.Admin));
    });
}

static void ConfigureMiddleware(WebApplication app)
{
    app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    app.UseMiddleware<CorrelationIdMiddleware>();
    if (app.Environment.IsDevelopment())
    {
        // Keep the developer page for local debugging only
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

    // --- Swagger gated by feature flag (EnableSwaggerUI) ---
    var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

    // Read once at startup: if AppConfig pipeline is active
    var isAppConfigActive = app.Configuration.GetValue<bool>("AppConfig:__Active");

    // Gate all /swagger* only when AppConfig is active; otherwise always allow
    app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase), branch =>
    {
        if (!isAppConfigActive)
        {
            // App Configuration is disabled -> don't gate, always allow Swagger locally.
            return;
        }

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

    // Register Swagger middlewares unconditionally
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

    // *** ADDED: options change hook -> update last refresh & log it
    var state = app.Services.GetRequiredService<ConfigRefreshState>();
    var monitor = app.Services.GetRequiredService<IOptionsMonitor<MyAppSettings>>();
    monitor.OnChange(v =>
    {
        state.LastRefreshUtc = DateTimeOffset.UtcNow;
        app.Logger.LogInformation("App Configuration refresh detected. SampleRatio={SampleRatio}", v.SampleRatio);
    });

    // *** ADDED: Diagnostics endpoints (hidden from Swagger)
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

    // Liveness: keep but hide from Swagger
    app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }))
       .AllowAnonymous()
       .ExcludeFromDescription();

    // Readiness: keep but hide from Swagger
    app.MapHealthChecks("/health")
       .AllowAnonymous()
       .ExcludeFromDescription();

    // CORS preflight catch-all: hide from Swagger
    app.MapMethods("{*path}", new[] { "OPTIONS" }, () => Results.NoContent())
       .RequireCors("Default")
       .ExcludeFromDescription();

    // Redirect root to Swagger: hide from Swagger
    app.MapGet("/", () => Results.Redirect("/swagger"))
       .AllowAnonymous()
       .ExcludeFromDescription();

    // Robots: hide from Swagger
    app.MapGet("/robots.txt", () => Results.Text("User-agent: *\nDisallow: /", "text/plain"))
       .AllowAnonymous()
       .ExcludeFromDescription();

    app.MapGet("/robots933456.txt", () => Results.Text("OK", "text/plain"))
       .AllowAnonymous()
       .ExcludeFromDescription();

    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    var apiBase = app.Configuration.GetValue<string>("Api:BasePath") ?? "/api";

    // Always register the endpoint; response is controlled by a feature flag.
    foreach (var desc in provider.ApiVersionDescriptions)
    {
        var group = app.MapGroup($"{apiBase}/{desc.GroupName}");

        group.MapGet("config-demo", (IConfiguration cfg) =>
        {
            // Values read at request time. They refresh after AppConfig:Sentinel changes.
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

static void TryAddAzureAppConfiguration(ConfigurationManager configuration)
{
    // Feature gate: only try if explicitly enabled
    var enabled = configuration.GetValue<bool>("AppConfig:Enabled");
    if (!enabled) { configuration["AppConfig:__Active"] = "false"; return; }

    var endpointValue = configuration["AppConfig:Endpoint"];
    if (string.IsNullOrWhiteSpace(endpointValue) || !Uri.TryCreate(endpointValue, UriKind.Absolute, out var endpoint))
    {
        configuration["AppConfig:__Active"] = "false";
        return;
    }

    // *** ADDED: honor optional Label + CacheSeconds; keep your defaults intact
    var label = configuration["AppConfig:Label"];
    var cacheSeconds = configuration.GetValue<int?>("AppConfig:CacheSeconds") ?? 90;

    const string sentinelKey = "AppConfig:Sentinel";

    try
    {
        // Non-interactive credentials chain: Env -> Managed Identity -> Azure CLI
        TokenCredential cred = new ChainedTokenCredential(
            new EnvironmentCredential(),
            new ManagedIdentityCredential(),
            new AzureCliCredential()
        );

        configuration.AddAzureAppConfiguration(options =>
        {
            options.Connect(endpoint, cred)
                   .ConfigureKeyVault(kv => kv.SetCredential(cred))   // <-- enable KV references

                   // *** ADDED: label-aware selection (falls back to "all" if label missing)
                   .Select(KeyFilter.Any, string.IsNullOrWhiteSpace(label) ? null : label)

                   .ConfigureRefresh(refresh =>
                       refresh.Register(sentinelKey, refreshAll: true)
                              // *** ADDED: cache TTL from config (default 90s to match your original)
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
        // Do not fail startup in dev/test; simply mark as inactive
        configuration["AppConfig:__Active"] = "false";
    }
}

// *** ADDED: simple POCO for strongly-typed settings
public sealed class MyAppSettings
{
    // Safe defaults when App Configuration is offline/unavailable
    public double SampleRatio { get; set; } = 1.0;
    public string? OtherSetting { get; set; }
}

// *** ADDED: state holder for last refresh timestamp (diagnostics)
public sealed class ConfigRefreshState
{
    public DateTimeOffset LastRefreshUtc { get; set; } = DateTimeOffset.MinValue;
}

// Required for WebApplicationFactory<Program> to locate the entry point during integration testing
public partial class Program { }
