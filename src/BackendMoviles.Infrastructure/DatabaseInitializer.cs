using BackendMoviles.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BackendMoviles.Infrastructure;

public static class DatabaseInitializer
{
    // Applies pending migrations on relational providers (SQLite) or creates the in-memory store.
    // Both paths also insert the daily tips declared with HasData.
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HelpMomDbContext>();
        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await context.Database.EnsureCreatedAsync(cancellationToken);
        }
    }
}
