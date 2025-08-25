using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;
using IncidentReportingSystem.Infrastructure.Attachments;
using IncidentReportingSystem.Infrastructure.Attachments.DevLoopback;
using IncidentReportingSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace IncidentReportingSystem.IntegrationTests.Utils;

/// <summary>
/// Dedicated factory for Attachments tests: runs migrations (via base),
/// registers Loopback storage for the loopback controller, cleans local storage root,
/// and seeds a single known Incident for Start to succeed.
/// </summary>
public class AttachmentsWebApplicationFactory : CustomWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        // 0) FORCE test-only configuration so the entire attachments module uses Loopback
        builder.ConfigureAppConfiguration((ctx, cfg) =>
        {
            var testRoot = Path.Combine(AppContext.BaseDirectory, "App_Data", "attachments-test");
            Directory.CreateDirectory(testRoot);

            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Attachments:Storage"] = "Loopback",         
                ["Api:PublicBaseUrl"] = "http://localhost",  
                ["Attachments:Loopback:Root"] = testRoot       
            });
        });

        builder.ConfigureServices(services =>
        {
            // 1) Force Loopback for ALL attachment ops (Start/Complete/Upload) in tests:
            var toRemove = services.Where(d => d.ServiceType == typeof(IAttachmentStorage)).ToList();
            foreach (var d in toRemove) services.Remove(d);

            services.AddSingleton<LoopbackAttachmentStorage>();
            services.AddSingleton<IAttachmentStorage>(sp => sp.GetRequiredService<LoopbackAttachmentStorage>());

            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();

            var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var root = cfg["Attachments:Loopback:Root"]
                   ?? Path.Combine(AppContext.BaseDirectory, "App_Data", "attachments-test");

            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
            Directory.CreateDirectory(root);

            var db = scope.ServiceProvider.GetRequiredService<IncidentReportingSystem.Infrastructure.Persistence.ApplicationDbContext>();
            SeedAttachmentsData(db, cfg);
        });
    }

    private static void SeedAttachmentsData(ApplicationDbContext db, IConfiguration cfg)
    {
        var idStr = cfg["Tests:ExistingIncidentId"];
        if (!Guid.TryParse(idStr, out var incidentId))
            return; // Not configured — skip.

        if (db.IncidentReports.Any(x => x.Id == incidentId))
            return; // Already there.

        // Reporter with unique email. Role "Admin" to satisfy your domain's role requirement.
        const string email = "attachments.seed@test.local";
        const string normalized = "ATTACHMENTS.SEED@TEST.LOCAL";
        var reporter = db.Users.FirstOrDefault(u => u.NormalizedEmail == normalized);
        if (reporter is null)
        {
            reporter = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                NormalizedEmail = normalized,
                PasswordHash = new byte[32],
                PasswordSalt = new byte[16],
                CreatedAtUtc = DateTime.UtcNow
            };
            reporter.SetRoles(new[] { "Admin" });
            db.Users.Add(reporter);
            db.SaveChanges();
        }

        // Create a valid Incident
        var incident = new IncidentReport(
            description: "Seeded incident for attachments IT",
            location: "Test-Location",
            reporterId: reporter.Id,
            category: (IncidentCategory)0,
            systemAffected: "TestSystem",
            severity: (IncidentSeverity)0,
            reportedAt: DateTime.UtcNow
        );

        // Force Id via reflection (private setter)
        var idProp = typeof(IncidentReport).GetProperty(
            "Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        idProp!.SetValue(incident, incidentId);

        db.IncidentReports.Add(incident);
        db.SaveChanges();
    }
}
