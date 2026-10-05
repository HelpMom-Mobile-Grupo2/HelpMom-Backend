using BackendMoviles.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace BackendMoviles.Infrastructure.Persistence;

public sealed class FamilyGroupRepository(HelpMomDbContext context)
    : Repository<FamilyGroup>(context), IFamilyGroupRepository
{
    public new Task<FamilyGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Entities.Include(group => group.Members).FirstOrDefaultAsync(group => group.Id == id, cancellationToken);

    public async Task<FamilyRole?> GetRoleForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var groups = await Entities.Include(group => group.Members).ToListAsync(cancellationToken);
        return groups.SelectMany(group => group.Members)
            .FirstOrDefault(member => member.UserId == userId)?.Role;
    }
}