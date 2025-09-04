using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentReportingSystem.IntegrationTests.Utils
{
    /// <summary>
    /// Truncates data safely by reading actual table names from EF Core metadata,
    /// after ensuring schema exists (Migrate).
    /// </summary>
    public sealed class DatabaseCleaner
    {
        private readonly IServiceProvider _sp;
        public DatabaseCleaner(IServiceProvider sp) => _sp = sp;

        public async Task TruncateAllAsync()
        {
            using var scope = _sp.CreateScope();
            var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var db = scope.ServiceProvider.GetRequiredService<IncidentReportingSystem.Infrastructure.Persistence.ApplicationDbContext>();

            var conn = cfg.GetConnectionString("DefaultConnection") ?? cfg["ConnectionStrings:DefaultConnection"] ?? "";
            var allow = Environment.GetEnvironmentVariable("ALLOW_TEST_DB_WIPE") == "true";
            if (!allow && !conn.Contains("test", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Refusing TRUNCATE on non-test database. Set ALLOW_TEST_DB_WIPE=true to override.");

            await db.Database.MigrateAsync();

            var entities = new[]
            {
                typeof(IncidentReportingSystem.Domain.Entities.Attachment),
                typeof(IncidentReportingSystem.Domain.Entities.IncidentComment),
                typeof(IncidentReportingSystem.Domain.Entities.IncidentReport),
            };

            var model = db.Model;
            var tableList = entities.Select(clr =>
            {
                var et = model.FindEntityType(clr) ?? throw new InvalidOperationException($"EntityType not found: {clr.Name}");
                var schema = et.GetSchema() ?? "public";
                var table = et.GetTableName() ?? throw new InvalidOperationException($"TableName not found: {clr.Name}");
                return $"\"{schema}\".\"{table}\"";
            }).ToArray();

            var sql = new StringBuilder("TRUNCATE ");
            sql.Append(string.Join(", ", tableList));
            sql.Append(" CASCADE;");

            await db.Database.ExecuteSqlRawAsync(sql.ToString());
        }
    }
}
