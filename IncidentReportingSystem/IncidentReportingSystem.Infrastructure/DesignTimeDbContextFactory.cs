using IncidentReportingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;

namespace IncidentReportingSystem.Infrastructure
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var conn = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
            if (string.IsNullOrWhiteSpace(conn))
                throw new InvalidOperationException("ConnectionStrings__Default is not set.");

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(conn)
                .Options;

            return (ApplicationDbContext)Activator.CreateInstance(typeof(ApplicationDbContext), options)!;
        }
    }
}
