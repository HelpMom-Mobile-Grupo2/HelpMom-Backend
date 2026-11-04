using System.Net;
using System.Net.Http.Json;
using BackendMoviles.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BackendMoviles.Tests;

// Runs the main flow against a real SQLite file so the migration and date queries are exercised.
public sealed class SqliteSmokeTests(SqliteApiTestFactory factory) : IClassFixture<SqliteApiTestFactory>
{
    [Fact]
    public async Task Migration_CreatesSchemaAndSeedsTips()
    {
        await factory.WithDbContextAsync(async context =>
        {
            Assert.Empty(await context.Database.GetPendingMigrationsAsync());
            Assert.Equal(DailyTipSeed.Tips.Count, await context.DailyTips.CountAsync());
        });
    }

    [Fact]
    public async Task CompanionFlow_WorksOnSqlite()
    {
        var (mother, partner) = await factory.CreateLinkedFamilyAsync();
        using var motherClient = factory.CreateClient(mother.AccessToken);
        using var partnerClient = factory.CreateClient(partner.AccessToken);
        var lastPeriod = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-170);
        Assert.Equal(HttpStatusCode.Created, (await motherClient.PostAsJsonAsync(
            "/api/pregnancy",
            new { lastMenstrualPeriod = lastPeriod, dueDate = lastPeriod.AddDays(280) })).StatusCode);

        await motherClient.PostAsJsonAsync("/api/alerts", new { type = 1 });
        await motherClient.PostAsJsonAsync("/api/telemetry", new { fetalHeartRateBpm = 190, temperatureCelsius = 36.8m });
        await motherClient.PostAsJsonAsync("/api/telemetry", new { fetalHeartRateBpm = 192, temperatureCelsius = 36.9m });
        await motherClient.PostAsJsonAsync("/api/triage", new { content = "Tengo náuseas" });
        await motherClient.PostAsJsonAsync("/api/triage", new { content = "Tengo fiebre" });

        var alerts = (await ApiTestFactory.ReadJsonAsync(await partnerClient.GetAsync("/api/alerts?status=pending")))
            .EnumerateArray().ToArray();
        Assert.Equal([4, 1], alerts.Select(alert => alert.GetProperty("type").GetInt32()));
        var respond = await partnerClient.PostAsJsonAsync(
            $"/api/alerts/{alerts[0].GetProperty("id").GetGuid()}/respond",
            new { response = 2 });
        Assert.Equal(HttpStatusCode.OK, respond.StatusCode);

        var history = (await ApiTestFactory.ReadJsonAsync(await motherClient.GetAsync("/api/telemetry/history?count=20")))
            .EnumerateArray().Select(item => item.GetProperty("fetalHeartRateBpm").GetInt32());
        Assert.Equal([192, 190], history);

        var triage = (await ApiTestFactory.ReadJsonAsync(await motherClient.GetAsync("/api/triage"))).EnumerateArray().ToArray();
        Assert.Equal([2, 1], triage.Select(item => item.GetProperty("urgency").GetInt32()));

        var summary = await ApiTestFactory.ReadJsonAsync(await partnerClient.GetAsync("/api/companion/summary"));
        Assert.Equal(24, summary.GetProperty("gestationalWeeks").GetInt32());
        Assert.Equal(3, summary.GetProperty("healthLight").GetInt32());

        var tips = (await ApiTestFactory.ReadJsonAsync(await partnerClient.GetAsync("/api/companion/tips/today")))
            .EnumerateArray().ToArray();
        Assert.Equal(3, tips.Length);
        var tipId = tips[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await partnerClient.PostAsync($"/api/companion/tips/{tipId}/done", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await partnerClient.PostAsync($"/api/companion/tips/{tipId}/done", null)).StatusCode);

        var login = await ApiTestFactory.ReadJsonAsync(await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/login",
            new { email = partner.Email, password = ApiTestFactory.Password }));
        Assert.Equal(mother.FamilyGroupId, login.GetProperty("familyGroupId").GetGuid());
    }
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
