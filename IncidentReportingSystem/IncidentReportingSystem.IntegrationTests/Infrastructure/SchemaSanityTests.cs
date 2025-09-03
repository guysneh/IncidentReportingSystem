using Dapper;
using FluentAssertions;
using IncidentReportingSystem.IntegrationTests.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace IncidentReportingSystem.IntegrationTests;

/// <summary>
/// Prints EF-resolved table names and compares to actual tables in PostgreSQL,
/// to quickly diagnose naming/scheme mismatches that cause 42P01.
/// </summary>
public sealed class SchemaSanityTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public SchemaSanityTests(TestAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Ef_Model_Table_Names_Exist_In_Database()
    {
        using var scope = _factory.Services.CreateScope();
        var cfg = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var db = scope.ServiceProvider.GetRequiredService<IncidentReportingSystem.Infrastructure.Persistence.ApplicationDbContext>();

        var connStr = cfg.GetConnectionString("DefaultConnection") ?? cfg["ConnectionStrings:DefaultConnection"]!;
        await using var cn = new NpgsqlConnection(connStr);
        await cn.OpenAsync();

        var entities = new[]
        {
            typeof(IncidentReportingSystem.Domain.Entities.Attachment),
            typeof(IncidentReportingSystem.Domain.Entities.IncidentComment),
            typeof(IncidentReportingSystem.Domain.Entities.IncidentReport),
        };

        foreach (var clr in entities)
        {
            var et = db.Model.FindEntityType(clr)!;
            var schema = et.GetSchema() ?? "public";
            var table = et.GetTableName()!;

            var exists = await cn.QuerySingleAsync<int>(
                @"select count(*) 
                      from information_schema.tables 
                      where table_schema = @schema and lower(table_name) = lower(@table)",
                new { schema, table });

            exists.Should().BeGreaterThan(0, $"Expected table '{schema}.{table}' to exist for entity {clr.Name}.");
        }
    }
}
