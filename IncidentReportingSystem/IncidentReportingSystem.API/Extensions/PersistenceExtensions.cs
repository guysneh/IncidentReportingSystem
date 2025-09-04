using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Infrastructure.Persistence;
using IncidentReportingSystem.Infrastructure.Persistence.Repositories;
using IncidentReportingSystem.Infrastructure.Persistence.Idempotency;
using Microsoft.EntityFrameworkCore;

namespace IncidentReportingSystem.API.Extensions;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["ConnectionStrings:DefaultConnection"];
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("No valid database connection string found.");

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

        // Repositories
        services.AddScoped<IIncidentReportRepository, IncidentReportRepository>();
        services.AddScoped<IIncidentCommentsRepository, IncidentCommentsRepository>();
        services.AddScoped<IUserRepository, UserRepository>();          

        // UoW + Idempotency
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();

        return services;
    }
}
