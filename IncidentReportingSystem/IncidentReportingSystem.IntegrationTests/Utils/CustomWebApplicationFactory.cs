using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using IncidentReportingSystem.Infrastructure.Persistence;

namespace IncidentReportingSystem.IntegrationTests.Utils;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            configBuilder.Sources.Clear();
            configBuilder
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();
        });

        builder.ConfigureServices((context, services) =>
        {
            // 1. Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // 2. Register new DbContext with test connection string
            var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            // 3. Build provider and run migrations
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();
        });
    }
}
