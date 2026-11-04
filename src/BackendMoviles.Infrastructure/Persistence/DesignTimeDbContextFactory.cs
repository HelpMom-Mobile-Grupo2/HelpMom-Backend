using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BackendMoviles.Infrastructure.Persistence;

// Used by `dotnet ef --context HelpMomDbContext` so SQLite migrations are generated without starting the API.
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<HelpMomDbContext>
{
    public HelpMomDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<HelpMomDbContext>()
            .UseSqlite(DependencyInjection.DefaultSqliteConnection)
            .Options);
}
