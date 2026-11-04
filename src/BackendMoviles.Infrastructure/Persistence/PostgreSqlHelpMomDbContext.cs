using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BackendMoviles.Infrastructure.Persistence;

// Same model as HelpMomDbContext; the separate type gives PostgreSQL its own migrations folder and snapshot.
public sealed class PostgreSqlHelpMomDbContext(DbContextOptions<PostgreSqlHelpMomDbContext> options)
    : HelpMomDbContext(options)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Npgsql only writes UTC values to timestamptz, and clients may send dates with any offset.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcDateTimeOffsetConverter>();
    }

    private sealed class UtcDateTimeOffsetConverter()
        : ValueConverter<DateTimeOffset, DateTimeOffset>(value => value.ToUniversalTime(), value => value);
}
