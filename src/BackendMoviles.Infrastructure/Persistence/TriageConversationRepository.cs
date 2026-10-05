using BackendMoviles.Domain.Triage;
using Microsoft.EntityFrameworkCore;

namespace BackendMoviles.Infrastructure.Persistence;

public sealed class TriageConversationRepository(HelpMomDbContext context)
    : Repository<TriageConversation>(context), ITriageConversationRepository
{
    public new Task<TriageConversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Entities.Include(conversation => conversation.Messages).FirstOrDefaultAsync(conversation => conversation.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<TriageConversation>> GetByMotherAsync(
        Guid motherUserId,
        CancellationToken cancellationToken = default) =>
        await Entities.Include(conversation => conversation.Messages)
            .Where(conversation => conversation.MotherUserId == motherUserId)
            .OrderByDescending(conversation => conversation.CreatedAt)
            .ToArrayAsync(cancellationToken);
}