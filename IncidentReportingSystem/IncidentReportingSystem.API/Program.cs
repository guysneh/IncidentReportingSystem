using FluentValidation;
using IncidentReportingSystem.API.Auth;
using IncidentReportingSystem.API.Converters;
using IncidentReportingSystem.API.Swagger;
using IncidentReportingSystem.Application.Common.Behaviors;
using IncidentReportingSystem.Application.IncidentReports.Commands.CreateIncidentReport;
using IncidentReportingSystem.Application.IncidentReports.Validators;
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
using Polly;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Always configure configuration first
ConfigureConfiguration(builder.Configuration, builder.Services);

if (args.Contains("--migrate"))
{
    // Only load the minimum required services for migration
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("No valid connection string found.");

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));

    var app = builder.Build();
    ApplyMigrations(app);
    return;
}

ConfigureServices(builder.Services, builder.Configuration);

var fullApp = builder.Build();

ConfigureMiddleware(fullApp, builder.Services);
fullApp.MapControllers();

fullApp.Run();


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
    // Add controllers and configure JSON serialization
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

    // Versioning
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

    // Swagger + JWT support
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


    // Register MediatR and FluentValidation
    services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(CreateIncidentReportCommandHandler).Assembly));
    services.AddValidatorsFromAssembly(typeof(CreateIncidentReportCommandValidator).Assembly);
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

    // Register database context
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("No valid database connection string found.");
    }

    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));

    services.AddScoped<IIncidentReportRepository, IncidentReportRepository>();

    // Register JWT authentication
    ConfigureJwtAuthentication(services, configuration);
}

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

static void ConfigureMiddleware(WebApplication app, IServiceCollection services)
{
    app.UseMiddleware<IncidentReportingSystem.API.Middleware.RequestLoggingMiddleware>();
    app.UseMiddleware<IncidentReportingSystem.API.Middleware.GlobalExceptionHandlingMiddleware>();
    var apiVersionDescriptionProvider = services.BuildServiceProvider()
    .GetRequiredService<IApiVersionDescriptionProvider>();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
        }
    });

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();
}

static void ApplyMigrations(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var policy = Policy
        .Handle<Exception>()
        .WaitAndRetry(
            retryCount: 10,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)), 
            onRetry: (exception, timespan, retryCount,context) =>
            {
                logger.LogWarning(exception, "Retry {RetryAttempt}: Failed to connect to DB. Waiting {Delay} before next try...", retryCount, timespan);
            });

    policy.Execute(() =>
    {
        logger.LogInformation("Attempting to apply migrations...");
        dbContext.Database.Migrate();
        logger.LogInformation("Migrations applied successfully.");
    });
}

// Required for WebApplicationFactory<Program> to locate the entry point during integration testing
public partial class Program { }