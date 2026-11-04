using System.Net;
using BackendMoviles.Infrastructure;
using BackendMoviles.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace BackendMoviles.Tests;

public sealed class DeploymentTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    [Theory]
    // Render's internal URL has no port.
    [InlineData("postgresql://helpmom_user:s3cr3t@dpg-abc123-a/helpmom", "dpg-abc123-a", 5432, "helpmom", "helpmom_user", "s3cr3t")]
    [InlineData("postgres://user:pass@db.example.test:6543/app_db", "db.example.test", 6543, "app_db", "user", "pass")]
    [InlineData("postgres://user:p%40ss%3Aword@localhost:5432/helpmom", "localhost", 5432, "helpmom", "user", "p@ss:word")]
    public void DatabaseUrl_IsConvertedToNpgsqlConnectionString(
        string databaseUrl,
        string host,
        int port,
        string database,
        string username,
        string password)
    {
        var builder = new NpgsqlConnectionStringBuilder(PostgreSqlConnectionString.FromDatabaseUrl(databaseUrl));

        Assert.Equal(host, builder.Host);
        Assert.Equal(port, builder.Port);
        Assert.Equal(database, builder.Database);
        Assert.Equal(username, builder.Username);
        Assert.Equal(password, builder.Password);
    }

    [Theory]
    [InlineData("require", SslMode.Require)]
    [InlineData("verify-full", SslMode.VerifyFull)]
    public void DatabaseUrl_KeepsSslMode(string sslMode, SslMode expected)
    {
        var connectionString = PostgreSqlConnectionString.FromDatabaseUrl(
            $"postgres://user:pass@db.example.test/helpmom?sslmode={sslMode}");

        Assert.Equal(expected, new NpgsqlConnectionStringBuilder(connectionString).SslMode);
    }

    [Theory]
    [InlineData("Host=localhost;Database=helpmom")]
    [InlineData("mysql://user:pass@localhost/helpmom")]
    [InlineData("postgres://localhost/helpmom")]
    [InlineData("postgres://user:pass@localhost")]
    [InlineData("postgres://user:pass@localhost/helpmom?sslmode=sometimes")]
    public void DatabaseUrl_RejectsInvalidValues(string databaseUrl)
    {
        Assert.Throws<InvalidOperationException>(() => PostgreSqlConnectionString.FromDatabaseUrl(databaseUrl));
    }

    [Fact]
    public void PostgreSqlProvider_UsesDatabaseUrlOverConnectionString()
    {
        using var services = BuildServices(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "PostgreSql",
            ["Database:ConnectionString"] = "Host=ignored;Database=ignored;Username=ignored",
            ["DATABASE_URL"] = "postgresql://helpmom_user:s3cr3t@dpg-abc123-a/helpmom"
        });
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HelpMomDbContext>();

        Assert.IsType<PostgreSqlHelpMomDbContext>(context);
        var builder = new NpgsqlConnectionStringBuilder(context.Database.GetConnectionString());
        Assert.Equal("dpg-abc123-a", builder.Host);
        Assert.Equal("helpmom", builder.Database);
    }

    [Fact]
    public void PostgreSqlProvider_FallsBackToConnectionString()
    {
        using var services = BuildServices(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "PostgreSql",
            ["Database:ConnectionString"] = "Host=localhost;Database=helpmom;Username=postgres"
        });
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HelpMomDbContext>();

        Assert.Equal("localhost", new NpgsqlConnectionStringBuilder(context.Database.GetConnectionString()).Host);
    }

    [Fact]
    public void PostgreSqlProvider_RequiresAConnection()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => BuildServices(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "PostgreSql"
        }));

        Assert.Contains("DATABASE_URL", exception.Message);
    }

    [Fact]
    public async Task Health_IsAnonymousAndChecksTheDatabase()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    private static ServiceProvider BuildServices(Dictionary<string, string?> settings) =>
        new ServiceCollection()
            .AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(settings).Build())
            .BuildServiceProvider();
}
