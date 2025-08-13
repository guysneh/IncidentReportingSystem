using FluentValidation;
using IncidentReportingSystem.API.Auth;
using IncidentReportingSystem.API.Converters;
using IncidentReportingSystem.API.Swagger;
using IncidentReportingSystem.Application;
using IncidentReportingSystem.Application.Common.Behaviors;
using IncidentReportingSystem.Domain.Enums;
using IncidentReportingSystem.Domain.Interfaces;
using IncidentReportingSystem.Infrastructure.IncidentReports.Repositories;
using IncidentReportingSystem.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;
using IncidentReportingSystem.API.Middleware;
using Microsoft.ApplicationInsights.Extensibility;
using IncidentReportingSystem.Infrastructure.Telemetry;

var builder = WebApplication.CreateBuilder(args);

// Always configure configuration first
ConfigureConfiguration(builder.Configuration, builder.Services);

// Services & DI
ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

// Middleware pipeline
ConfigureMiddleware(app);

// Map controllers and attach CORS policy if configured
var controllers = app.MapControllers();
var allowedOrigins = GetCorsAllowedOrigins(app.Configuration);
if (!string.IsNullOrWhiteSpace(allowedOrigins))
{
    controllers.RequireCors("Default");
}

app.Run();


// ------------------------------
// METHODS
// ------------------------------
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

    services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
}

static bool IsRunningInDocker()
{
    return File.Exists("/.dockerenv") || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
}

static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // --- Application Insights ---
    // Reads APPLICATIONINSIGHTS_CONNECTION_STRING from configuration (already wired via Key Vault / pipeline).
    services.AddApplicationInsightsTelemetry(); // ASP.NET AI registration (belongs in API layer)
    // Stamp a stable cloud role name for this service to enable clean filtering in KQL.
    services.AddSingleton<ITelemetryInitializer>(_ => new TelemetryInitializer("incident-api"));
    // Drop noisy 404s for "/" and robots*.txt
    services.AddApplicationInsightsTelemetryProcessor<IgnoreNoiseTelemetryProcessor>();

    // --- Rate limiting (global) ---
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

    // --- Health checks ---
    services.AddHealthChecks()
        .AddNpgSql(configuration["ConnectionStrings:DefaultConnection"]);

    // --- Controllers & JSON ---
    services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new EnumConverterFactory());
        })
        .ConfigureApiBehaviorOptions(options =>
        {
            // Customize validation error response
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

    // --- API Versioning & Explorer ---
    services.AddApiVersioning(options =>
    {
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.ReportApiVersions = true;
    });

    services.AddVersionedApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    // --- Swagger (+ JWT support) ---
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
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // --- MediatR + Validation pipeline ---
    services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyReference).Assembly));
    services.AddValidatorsFromAssembly(typeof(ApplicationAssemblyReference).Assembly);
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

    // --- Database (EF Core / Npgsql) ---
    var connectionString = configuration["ConnectionStrings:DefaultConnection"];
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("No valid database connection string found.");
    }

    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));

    services.AddScoped<IIncidentReportRepository, IncidentReportRepository>();

    // --- AuthN/Z (JWT) ---
    ConfigureJwtAuthentication(services, configuration);

    // --- CORS (opt-in) ---
    ConfigureCors(services, configuration);
}

static void ConfigureCors(IServiceCollection services, IConfiguration configuration)
{
    // Accept both notations. In Azure App Settings: "Cors__AllowedOrigins" maps to "Cors:AllowedOrigins".
    var allowedOrigins = GetCorsAllowedOrigins(configuration);
    if (string.IsNullOrWhiteSpace(allowedOrigins))
        return;

    var origins = allowedOrigins
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    if (origins.Length == 0)
        return;

    services.AddCors(options =>
    {
        options.AddPolicy("Default", policy =>
        {
            policy
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod();
            // Do not call .AllowCredentials() unless cookies are truly required with specific origins.
        });
    });
}

static string? GetCorsAllowedOrigins(IConfiguration configuration) =>
    configuration["Cors:AllowedOrigins"] ?? configuration["Cors__AllowedOrigins"];

static void ConfigureJwtAuthentication(IServiceCollection services, IConfiguration configuration)
{
    var jwtSecret = configuration["Jwt:Secret"];
    if (string.IsNullOrWhiteSpace(jwtSecret))
        throw new InvalidOperationException("Missing Jwt:Secret in configuration.");

    services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var jwtSettings = configuration.GetSection("Jwt");
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
            )
        };
    });

    services.AddAuthorization();
}

static void ConfigureMiddleware(WebApplication app)
{
    // Cross-cutting middleware
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseRateLimiter();

    // Health endpoint (simple) – CORS not required here
    app.MapHealthChecks("/health");

    // Logging & global error handling
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

    // Swagger (enabled in Dev or via EnableSwagger=true)
    var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    var showSwagger = app.Configuration.GetValue<bool>("EnableSwagger", false);
    if (showSwagger || app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
            }
        });
    }

    app.UseHttpsRedirection();

    // CORS: enable only if configured. Must be before MapControllers.
    var allowedOrigins = GetCorsAllowedOrigins(app.Configuration);
    if (!string.IsNullOrWhiteSpace(allowedOrigins))
    {
        app.UseCors("Default");
    }

    // AuthN/Z
    app.UseAuthentication();
    app.UseAuthorization();

    // Convenience endpoints
    // Redirect root ("/") to Swagger UI to avoid 404 and improve discoverability
    app.MapGet("/", () => Results.Redirect("/swagger"))
       .WithName("RootRedirect")
       .WithSummary("Redirects root to Swagger UI")
       .WithDescription("Prevents 404s for '/' and improves discoverability.");

    // Minimal robots.txt to stop noisy 404s from bots
    app.MapGet("/robots.txt", () => Results.Text("User-agent: *\nDisallow: /", "text/plain"))
       .WithName("RobotsTxt");

    // Some platforms ping a random robots file; return OK to reduce AI noise
    app.MapGet("/robots933456.txt", () => Results.Text("OK", "text/plain"))
       .WithName("RobotsRandomTxt");
}

// Required for WebApplicationFactory<Program> to locate the entry point during integration testing
public partial class Program { }
