using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Xunit;

namespace BackendMoviles.Tests;

// Runs the main flow against a real SQLite file.
public sealed class SqliteSmokeTests(SqliteApiTestFactory factory) : IClassFixture<SqliteApiTestFactory>
{
    [Fact]
    public Task Migration_CreatesSchemaAndSeedsTips() => RelationalSmokeFlow.AssertMigratedAndSeededAsync(factory);

    [Fact]
    public Task CompanionFlow_WorksOnSqlite() => RelationalSmokeFlow.RunCompanionFlowAsync(factory);
}

public sealed class SqliteApiTestFactory : ApiTestFactory
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"helpmom-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("Database:ConnectionString", $"Data Source={_databasePath}");
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        SqliteConnection.ClearAllPools();
        try
        {
            File.Delete(_databasePath);
        }
        catch (IOException)
        {
            // A leftover file in the temp folder is harmless.
        }
    }
}
