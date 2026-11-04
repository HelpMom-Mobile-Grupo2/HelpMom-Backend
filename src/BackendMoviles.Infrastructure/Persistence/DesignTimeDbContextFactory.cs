using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BackendMoviles.Infrastructure.Persistence;

// Used by `dotnet ef` so migrations are generated for SQLite without starting the API (and its JWT checks).
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<HelpMomDbContext>
{
    public HelpMomDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<HelpMomDbContext>()
            .UseSqlite(DependencyInjection.DefaultSqliteConnection)
            .Options);
}
