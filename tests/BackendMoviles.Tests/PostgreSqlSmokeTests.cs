using Microsoft.AspNetCore.Hosting;
using Npgsql;
using Xunit;

namespace BackendMoviles.Tests;

// Runs the main flow against a real PostgreSQL server. Skipped unless HELPMOM_TEST_POSTGRES holds a connection
// string, for example: Host=localhost;Port=5432;Username=postgres;Password=postgres
public sealed class PostgreSqlSmokeTests(PostgreSqlApiTestFactory factory) : IClassFixture<PostgreSqlApiTestFactory>
{
    [PostgreSqlFact]
    public Task Migration_CreatesSchemaAndSeedsTips() => RelationalSmokeFlow.AssertMigratedAndSeededAsync(factory);

    [PostgreSqlFact]
    public Task CompanionFlow_WorksOnPostgreSql() => RelationalSmokeFlow.RunCompanionFlowAsync(factory);
}

public sealed class PostgreSqlFactAttribute : FactAttribute
{
    public const string ConnectionStringVariable = "HELPMOM_TEST_POSTGRES";

    public PostgreSqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionStringVariable)))
        {
            Skip = $"Define {ConnectionStringVariable} to run the PostgreSQL smoke tests.";
        }
    }
}

public sealed class PostgreSqlApiTestFactory : ApiTestFactory
{
    // Each run gets its own database on the server, dropped when the fixture is disposed.
    private readonly string _connectionString = new NpgsqlConnectionStringBuilder(
        Environment.GetEnvironmentVariable(PostgreSqlFactAttribute.ConnectionStringVariable) ?? string.Empty)
    {
        Database = $"helpmom_tests_{Guid.NewGuid():N}"
    }.ConnectionString;

    private bool _started;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Database:Provider", "PostgreSql");
        builder.UseSetting("Database:ConnectionString", _connectionString);
        builder.UseSetting("DATABASE_URL", string.Empty);
        _started = true;
    }

    public override async ValueTask DisposeAsync()
    {
        if (_started)
        {
            await WithDbContextAsync(context => context.Database.EnsureDeletedAsync());
        }

        await base.DisposeAsync();
    }
}
