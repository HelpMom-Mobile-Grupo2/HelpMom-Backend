namespace BackendMoviles.Domain.Pregnancy;

public interface IPregnancyRepository
{
    Task<PregnancyRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PregnancyRecord>> GetByMotherAsync(Guid motherUserId, CancellationToken cancellationToken = default);
    Task AddAsync(PregnancyRecord pregnancy, CancellationToken cancellationToken = default);
    Task DeleteAsync(PregnancyRecord pregnancy, CancellationToken cancellationToken = default);
}