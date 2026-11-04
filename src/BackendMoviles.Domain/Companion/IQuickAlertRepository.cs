namespace BackendMoviles.Domain.Companion;

public interface IQuickAlertRepository
{
    Task<QuickAlert?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<QuickAlert>> GetByFamilyAsync(
        Guid familyGroupId,
        bool pendingOnly,
        CancellationToken cancellationToken = default);
    Task<bool> HasPendingSinceAsync(
        Guid familyGroupId,
        QuickAlertType type,
        string message,
        DateTimeOffset since,
        CancellationToken cancellationToken = default);
    Task AddAsync(QuickAlert alert, CancellationToken cancellationToken = default);
}
