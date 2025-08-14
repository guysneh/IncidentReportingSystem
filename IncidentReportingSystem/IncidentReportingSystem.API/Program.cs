using FluentValidation;
using IncidentReportingSystem.API.Auth;
using IncidentReportingSystem.API.Converters;
using IncidentReportingSystem.API.Middleware;
using IncidentReportingSystem.API.Swagger;
using IncidentReportingSystem.Application;
using IncidentReportingSystem.Application.Authentication;
using IncidentReportingSystem.Application.Common.Behaviors;
using IncidentReportingSystem.Domain.Enums;
using IncidentReportingSystem.Domain.Interfaces;
using IncidentReportingSystem.Infrastructure.Authentication;
using IncidentReportingSystem.Infrastructure.IncidentReports.Repositories;
using IncidentReportingSystem.Infrastructure.Persistence;
using IncidentReportingSystem.Infrastructure.Telemetry;
using MediatR;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// 1) Configuration (keep first)
ConfigureConfiguration(builder.Configuration, builder.Services);

// 2) Services & DI
ConfigureServices(builder.Services, builder.Configuration, builder.Environment);

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

    // Avoid trying to read non-mounted files in container
    if (!IsRunningInDocker())
    {
        configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        configuration.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);
    }

    configuration.AddEnvironmentVariables();
    services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
    services.Configure<PasswordHashingOptions>(configuration.GetSection("Auth:PasswordHashing"));
}

static bool IsRunningInDocker() =>
    File.Exists("/.dockerenv") || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

static void ConfigureServices(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
{
    // Application Insights
    services.AddApplicationInsightsTelemetry();
    services.AddSingleton<ITelemetryInitializer>(_ => new TelemetryInitializer("incident-api"));
    services.AddApplicationInsightsTelemetryProcessor<IgnoreNoiseTelemetryProcessor>();

    // Rate limiting
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

    // Health checks
    services.AddHealthChecks()
        .AddNpgSql(configuration["ConnectionStrings:DefaultConnection"]);

    // Controllers + JSON
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

    // CORS
    ConfigureCors(services, configuration, env);

    // API versioning
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

    // Swagger
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

    // MediatR + FluentValidation
    services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyReference).Assembly));
    services.AddValidatorsFromAssembly(typeof(ApplicationAssemblyReference).Assembly);
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

    // DbContext
    var connectionString = configuration["ConnectionStrings:DefaultConnection"];
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("No valid database connection string found.");

    services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
    services.AddScoped<IIncidentReportRepository, IncidentReportRepository>();
    services.AddScoped<IPasswordHasher, PasswordHasherPBKDF2>();

    // AuthN/AuthZ
    ConfigureJwtAuthentication(services, configuration);
}
// helper to read and split origins from config
static string[] GetAllowedOrigins(IConfiguration config) =>
    (config["Cors:AllowedOrigins"] ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

// register CORS exactly once, no default policy elsewhere
static void ConfigureCors(IServiceCollection services, IConfiguration config, IHostEnvironment env)
{
    var origins = GetAllowedOrigins(config);

    services.AddCors(options =>
    {
        options.AddPolicy("Default", b =>
        {
            if (origins.Length > 0)
            {
                b.WithOrigins(origins)
                 .AllowAnyHeader()
                 .AllowAnyMethod()
                 .AllowCredentials();
            }
            else if (env.IsDevelopment())
            {
                b.AllowAnyOrigin()
                 .AllowAnyHeader()
                 .AllowAnyMethod();
            }
            else
            {
                b.SetIsOriginAllowed(_ => false)
                 .AllowAnyHeader()
                 .AllowAnyMethod();
            }
        });
    });
}

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

                RoleClaimType = "role",
                NameClaimType = "sub"
            };
        });

    services.AddAuthorization(options =>
    {
        options.AddPolicy("CanReadIncidents", p => p.RequireRole("User", "Admin"));
        options.AddPolicy("CanCreateIncident", p => p.RequireRole("User", "Admin"));
        options.AddPolicy("CanManageIncidents", p => p.RequireRole("Admin"));
    });
}

static void ConfigureMiddleware(WebApplication app)
{
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseRouting();
    app.UseCors("Default");
    app.UseRateLimiter();
    app.MapHealthChecks("/health");
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

    var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    var showSwagger = app.Configuration.GetValue<bool>("EnableSwagger", false);
    if (showSwagger || app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                                        description.GroupName.ToUpperInvariant());
            }
        });
    }

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers().RequireCors("Default");

    app.MapMethods("{*path}", new[] { "OPTIONS" }, () => Results.NoContent())
       .RequireCors("Default");

    app.MapGet("/", () => Results.Redirect("/swagger")).WithName("RootRedirect");
    app.MapGet("/robots.txt", () => Results.Text("User-agent: *\nDisallow: /", "text/plain")).WithName("RobotsTxt");
    app.MapGet("/robots933456.txt", () => Results.Text("OK", "text/plain")).WithName("RobotsRandomTxt");
}


// Required for WebApplicationFactory<Program> to locate the entry point during integration testing
public partial class Program { }
