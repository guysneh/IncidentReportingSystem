using IncidentReportingSystem.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1) Configuration + options binding + AppConfig/FeatureFlags providers
builder.AddConfigurationAndBindOptions();

// 2) Services & DI
builder.Services
    .AddWebApi()
    .AddCorsPolicy(builder.Configuration)
    .AddHealthAndRateLimiting(builder.Configuration)
    .AddPersistence(builder.Configuration)
    .AddJwtAuth(builder.Configuration)
    .AddCurrentUserAccessor()
    .AddAttachmentsModule(builder.Configuration);

// Telemetry (OpenTelemetry + Azure Monitor) 
builder.Services.AddAppTelemetry(builder.Configuration, builder.Environment);

// 3) Build
var app = builder.Build();

// 4) Pipeline
app.UseAppPipeline();

app.Run();

// Required for WebApplicationFactory<Program> to locate the entry point during integration testing
public partial class Program { }
