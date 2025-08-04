using FluentValidation;
using IncidentReportingSystem.Application.Common.Behaviors;
using IncidentReportingSystem.Application.IncidentReports.Commands.CreateIncidentReport;
using IncidentReportingSystem.Domain.Interfaces;
using IncidentReportingSystem.Infrastructure.IncidentReports.Repositories;
using IncidentReportingSystem.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Load configuration from JSON and environment variables
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Register all application services
ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

// Configure middleware (logging, swagger, etc.)
ConfigureMiddleware(app);

// Map API endpoints
app.MapControllers();

// Apply migrations if "--migrate" flag is passed
if (args.Contains("--migrate"))
{
    ApplyMigrations(app);
    return;
}

// Start the application
app.Run();


// --------------------------
// METHODS
// --------------------------

static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // Add basic framework services
    services.AddControllers();
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();

    // Add MediatR & pipeline behaviors
    services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(CreateIncidentReportCommandHandler).Assembly));

    services.AddValidatorsFromAssembly(typeof(CreateIncidentReportCommandValidator).Assembly);
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

    // Resolve connection string: try appsettings, then environment
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        connectionString = configuration["CONNECTION_STRING"];
    }

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("No valid database connection string found.");
    }

    // Register EF Core DbContext
    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));

    // Register custom repositories
    services.AddScoped<IIncidentReportRepository, IncidentReportRepository>();
}

static void ConfigureMiddleware(WebApplication app)
{
    app.UseMiddleware<IncidentReportingSystem.API.Middleware.RequestLoggingMiddleware>();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
}

static void ApplyMigrations(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var pending = db.Database.GetPendingMigrations().ToList();
    if (pending.Any())
    {
        Console.WriteLine("Applying pending migrations...");
        db.Database.Migrate();
    }
    else
    {
        Console.WriteLine("No pending migrations. Skipping.");
    }
}
