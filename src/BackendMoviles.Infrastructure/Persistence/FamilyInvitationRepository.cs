using BackendMoviles.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace BackendMoviles.Infrastructure.Persistence;

public sealed class FamilyInvitationRepository(HelpMomDbContext context)
    : Repository<FamilyInvitation>(context), IFamilyInvitationRepository
{
    public Task<FamilyInvitation?> GetLatestByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        Entities.Where(invitation => invitation.Code == code)
            .OrderByDescending(invitation => invitation.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyCollection<FamilyInvitation>> GetActiveByFamilyAsync(
        Guid familyGroupId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default) =>
        await Entities.Where(invitation => invitation.FamilyGroupId == familyGroupId
                && !invitation.IsUsed
                && invitation.ExpiresAt > now)
            .ToArrayAsync(cancellationToken);

    public Task<bool> ActiveCodeExistsAsync(string code, DateTimeOffset now, CancellationToken cancellationToken = default) =>
        Entities.AnyAsync(
            invitation => invitation.Code == code && !invitation.IsUsed && invitation.ExpiresAt > now,
            cancellationToken);
}
