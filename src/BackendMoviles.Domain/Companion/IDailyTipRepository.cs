namespace BackendMoviles.Domain.Companion;

public interface IDailyTipRepository
{
    Task<DailyTip?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DailyTip>> GetForWeekAsync(int? week, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Guid>> GetCompletedTipIdsAsync(
        Guid partnerUserId,
        DateOnly date,
        CancellationToken cancellationToken = default);
    Task AddCompletionAsync(TipCompletion completion, CancellationToken cancellationToken = default);
}
