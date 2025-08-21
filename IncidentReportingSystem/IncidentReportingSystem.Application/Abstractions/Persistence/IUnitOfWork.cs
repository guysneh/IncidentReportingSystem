namespace IncidentReportingSystem.Application.Abstractions.Persistence
{
    /// <summary>
    /// Abstraction for committing changes to the persistence store.
    /// </summary>
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
