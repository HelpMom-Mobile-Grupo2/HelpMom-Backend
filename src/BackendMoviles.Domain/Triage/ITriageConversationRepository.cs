namespace BackendMoviles.Domain.Triage;

public interface ITriageConversationRepository
{
    Task<TriageConversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TriageConversation>> GetByMotherAsync(Guid motherUserId, CancellationToken cancellationToken = default);
    Task AddAsync(TriageConversation conversation, CancellationToken cancellationToken = default);
}