using System.Net;
using System.Net.Http.Json;
using BackendMoviles.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BackendMoviles.Tests;

// Main flow shared by the SQLite and PostgreSQL smoke tests, so both migrations and their date queries are exercised.
public static class RelationalSmokeFlow
{
    public static Task AssertMigratedAndSeededAsync(ApiTestFactory factory) =>
        factory.WithDbContextAsync(async context =>
        {
            Assert.Empty(await context.Database.GetPendingMigrationsAsync());
            Assert.Equal(DailyTipSeed.Tips.Count, await context.DailyTips.CountAsync());
        });

    public static async Task RunCompanionFlowAsync(ApiTestFactory factory)
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

        // Clients may send dates with any offset; PostgreSQL stores them in UTC and the instant must not change.
        // Whole seconds, because PostgreSQL keeps microseconds and .NET ticks are finer.
        var measuredAt = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds())
            .ToOffset(TimeSpan.FromHours(-5));
        var withOffset = await motherClient.PostAsJsonAsync(
            "/api/telemetry",
            new { fetalHeartRateBpm = 140, temperatureCelsius = 36.6m, measuredAt });
        Assert.Equal(HttpStatusCode.Created, withOffset.StatusCode);
        var stored = (await ApiTestFactory.ReadJsonAsync(await motherClient.GetAsync("/api/telemetry/history?count=20")))
            .EnumerateArray().Last();
        Assert.Equal(140, stored.GetProperty("fetalHeartRateBpm").GetInt32());
        Assert.Equal(measuredAt, stored.GetProperty("measuredAt").GetDateTimeOffset());
    }
}
