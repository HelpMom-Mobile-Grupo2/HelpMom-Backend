using BackendMoviles.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace BackendMoviles.Infrastructure.Persistence;

public sealed class UserRepository(HelpMomDbContext context)
    : Repository<User>(context), IUserRepository
{
    public Task<User?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default) =>
        Entities.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
}