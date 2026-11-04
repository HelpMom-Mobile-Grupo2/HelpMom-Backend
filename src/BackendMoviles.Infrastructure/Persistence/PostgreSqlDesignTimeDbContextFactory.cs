using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BackendMoviles.Infrastructure.Persistence;

// Used by `dotnet ef --context PostgreSqlHelpMomDbContext`. Generating migrations does not open a connection,
// so the placeholder connection string is never used to reach a server.
public sealed class PostgreSqlDesignTimeDbContextFactory : IDesignTimeDbContextFactory<PostgreSqlHelpMomDbContext>
{
    public PostgreSqlHelpMomDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<PostgreSqlHelpMomDbContext>()
            .UseNpgsql("Host=localhost;Database=helpmom;Username=postgres")
            .Options);
}
