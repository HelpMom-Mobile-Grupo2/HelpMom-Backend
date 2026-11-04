using BackendMoviles.Domain.Companion;
using Microsoft.EntityFrameworkCore;

namespace BackendMoviles.Infrastructure.Persistence;

public sealed class QuickAlertRepository(HelpMomDbContext context)
    : Repository<QuickAlert>(context), IQuickAlertRepository
{
    public async Task<IReadOnlyCollection<QuickAlert>> GetByFamilyAsync(
        Guid familyGroupId,
        bool pendingOnly,
        CancellationToken cancellationToken = default)
    {
        var query = Entities.Where(alert => alert.FamilyGroupId == familyGroupId);
        if (pendingOnly)
        {
            query = query.Where(alert => alert.Status == QuickAlertStatus.Pending);
        }

        return await query.OrderByDescending(alert => alert.CreatedAt).ToArrayAsync(cancellationToken);
    }
}
