namespace BackendMoviles.Domain.Identity;

public interface IFamilyInvitationRepository
{
    Task<FamilyInvitation?> GetLatestByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<FamilyInvitation>> GetActiveByFamilyAsync(
        Guid familyGroupId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
    Task<bool> ActiveCodeExistsAsync(string code, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task AddAsync(FamilyInvitation invitation, CancellationToken cancellationToken = default);
}
