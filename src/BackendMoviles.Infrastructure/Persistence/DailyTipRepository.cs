using BackendMoviles.Domain.Companion;
using Microsoft.EntityFrameworkCore;

namespace BackendMoviles.Infrastructure.Persistence;

public sealed class DailyTipRepository(HelpMomDbContext context)
    : Repository<DailyTip>(context), IDailyTipRepository
{
    public async Task<IReadOnlyList<DailyTip>> GetForWeekAsync(int? week, CancellationToken cancellationToken = default)
    {
        var query = Entities.AsQueryable();
        if (week is int current)
        {
            query = query.Where(tip => tip.FromWeek <= current && tip.ToWeek >= current);
        }

        return await query.OrderBy(tip => tip.FromWeek).ThenBy(tip => tip.Id).ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>> GetCompletedTipIdsAsync(
        Guid partnerUserId,
        DateOnly date,
        CancellationToken cancellationToken = default) =>
        await Context.TipCompletions
            .Where(completion => completion.PartnerUserId == partnerUserId && completion.Date == date)
            .Select(completion => completion.TipId)
            .ToArrayAsync(cancellationToken);

    public async Task AddCompletionAsync(TipCompletion completion, CancellationToken cancellationToken = default) =>
        await Context.TipCompletions.AddAsync(completion, cancellationToken);
}
