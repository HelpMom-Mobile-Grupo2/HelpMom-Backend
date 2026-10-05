namespace BackendMoviles.Domain.Common;

public interface IUnitOfWork
{
    void Add(Entity entity);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}