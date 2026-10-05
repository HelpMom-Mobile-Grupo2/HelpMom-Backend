using BackendMoviles.Domain.Pregnancy;
using Microsoft.EntityFrameworkCore;

namespace BackendMoviles.Infrastructure.Persistence;

public sealed class PregnancyRepository(HelpMomDbContext context)
    : Repository<PregnancyRecord>(context), IPregnancyRepository
{
    public new Task<PregnancyRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Entities.Include(pregnancy => pregnancy.Symptoms).FirstOrDefaultAsync(pregnancy => pregnancy.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<PregnancyRecord>> GetByMotherAsync(
        Guid motherUserId,
        CancellationToken cancellationToken = default) =>
        await Entities.Include(pregnancy => pregnancy.Symptoms)
            .Where(pregnancy => pregnancy.MotherUserId == motherUserId)
            .OrderByDescending(pregnancy => pregnancy.CreatedAt)
            .ToArrayAsync(cancellationToken);

    public Task DeleteAsync(PregnancyRecord pregnancy, CancellationToken cancellationToken = default)
    {
        Delete(pregnancy);
        return Task.CompletedTask;
    }
}