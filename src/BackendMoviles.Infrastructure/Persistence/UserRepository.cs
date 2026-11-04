using BackendMoviles.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace BackendMoviles.Infrastructure.Persistence;

public sealed class UserRepository(HelpMomDbContext context)
    : Repository<User>(context), IUserRepository
{
    public async Task<IReadOnlyCollection<User>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var distinctIds = ids.Distinct().ToArray();
        return await Entities.Where(user => distinctIds.Contains(user.Id)).ToArrayAsync(cancellationToken);
    }

    public Task<User?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default) =>
        Entities.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
}
