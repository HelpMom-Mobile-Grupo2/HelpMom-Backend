using BackendMoviles.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BackendMoviles.Infrastructure;

public static class DatabaseInitializer
{
    // Creates the schema and applies the seed data (daily tips) declared with HasData.
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HelpMomDbContext>();
        await context.Database.EnsureCreatedAsync(cancellationToken);
    }
}
