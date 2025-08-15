using IncidentReportingSystem.Domain.Interfaces;

namespace IncidentReportingSystem.Infrastructure.Persistence
{
    /// <summary>EF-backed UnitOfWork.</summary>
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _db;
        public UnitOfWork(ApplicationDbContext db) => _db = db;
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
            _db.SaveChangesAsync(cancellationToken);
    }
}
