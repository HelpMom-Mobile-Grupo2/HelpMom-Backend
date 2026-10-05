namespace BackendMoviles.Domain.Identity;

public interface IFamilyGroupRepository
{
    Task<FamilyGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FamilyRole?> GetRoleForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(FamilyGroup familyGroup, CancellationToken cancellationToken = default);
}