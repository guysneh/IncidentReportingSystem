using FluentValidation;
using IncidentReportingSystem.Application.Common.Behaviors;
using IncidentReportingSystem.Application.IncidentReports.Commands.CreateIncidentReport;
using IncidentReportingSystem.Domain.Interfaces;
using IncidentReportingSystem.Infrastructure.IncidentReports.Repositories;
using IncidentReportingSystem.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Register services
ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

// Configure middleware
ConfigureMiddleware(app);

// Map controllers
app.MapControllers();

// Apply migrations if '--migrate' flag is passed
if (args.Contains("--migrate"))
{
    ApplyMigrations(app);
    return;
}

app.Run();


// ------------------------------
// METHODS
// ------------------------------

static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // Add controllers + enums as strings
    services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(c =>
    {
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


    // MediatR and validation
    services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(CreateIncidentReportCommandHandler).Assembly));

    services.AddValidatorsFromAssembly(typeof(CreateIncidentReportCommandValidator).Assembly);
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

    // Connection string from appsettings or env
    var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                           configuration["CONNECTION_STRING"];
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("No valid database connection string found.");
    }

    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));

    services.AddScoped<IIncidentReportRepository, IncidentReportRepository>();

    // JWT Authentication
    var jwtSecret = configuration["Jwt:SecretKey"];
    if (string.IsNullOrWhiteSpace(jwtSecret))
        throw new InvalidOperationException("Missing Jwt:SecretKey in configuration.");

    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
            };
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<Program>>();

                    logger.LogError(context.Exception, "JWT Authentication failed");
                    return Task.CompletedTask;
                }
            };
        });

    services.AddAuthorization();
}

static void ConfigureMiddleware(WebApplication app)
{
    app.UseMiddleware<IncidentReportingSystem.API.Middleware.RequestLoggingMiddleware>();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();
}

static void ApplyMigrations(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var pending = db.Database.GetPendingMigrations().ToList();
    if (pending.Any())
    {
        logger.LogInformation("Applying {Count} pending migrations...", pending.Count);
        db.Database.Migrate();
        logger.LogInformation("Migrations applied successfully.");
    }
    else
    {
        logger.LogInformation("No pending migrations. Skipping.");
    }
}
