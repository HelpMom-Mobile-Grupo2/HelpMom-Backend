using BackendMoviles.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace BackendMoviles.Infrastructure.Persistence;

public class Repository<TEntity>(HelpMomDbContext context) where TEntity : Entity
{
    protected DbSet<TEntity> Entities => context.Set<TEntity>();

    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Entities.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await Entities.AddAsync(entity, cancellationToken);

    public void Delete(TEntity entity) => Entities.Remove(entity);
}