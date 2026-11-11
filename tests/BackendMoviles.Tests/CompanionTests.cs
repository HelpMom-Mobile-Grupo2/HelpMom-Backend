using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BackendMoviles.Application.Companion;
using BackendMoviles.Domain.Companion;
using BackendMoviles.Infrastructure.Persistence;
using Xunit;

namespace BackendMoviles.Tests;

public sealed class CompanionTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    private static readonly string[] SummaryProperties =
        ["motherName", "gestationalWeeks", "gestationalDays", "healthLight", "healthSummary", "measuredAt"];

    [Fact]
    public async Task Summary_WithPregnancyAndNoMeasurement_ReturnsAgeAndNullHealthFields()
    {
        var (mother, partner) = await factory.CreateLinkedFamilyAsync();
        await CreatePregnancyAsync(mother, daysSinceLastPeriod: 170);
        using var partnerClient = factory.CreateClient(partner.AccessToken);

        var response = await partnerClient.GetAsync("/api/companion/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await ApiTestFactory.ReadJsonAsync(response);
        Assert.Equal("Daniela Gómez", summary.GetProperty("motherName").GetString());
        Assert.Equal(24, summary.GetProperty("gestationalWeeks").GetInt32());
        Assert.Equal(2, summary.GetProperty("gestationalDays").GetInt32());
        Assert.Equal(JsonValueKind.Null, summary.GetProperty("healthLight").ValueKind);
        Assert.Equal(JsonValueKind.Null, summary.GetProperty("healthSummary").ValueKind);
        Assert.Equal(JsonValueKind.Null, summary.GetProperty("measuredAt").ValueKind);
    }

    [Fact]
    public async Task Summary_WithoutPregnancy_ReturnsNullGestationalAge()
    {
        var (_, partner) = await factory.CreateLinkedFamilyAsync();
        using var partnerClient = factory.CreateClient(partner.AccessToken);

        var summary = await ApiTestFactory.ReadJsonAsync(await partnerClient.GetAsync("/api/companion/summary"));

        Assert.Equal(JsonValueKind.Null, summary.GetProperty("gestationalWeeks").ValueKind);
        Assert.Equal(JsonValueKind.Null, summary.GetProperty("gestationalDays").ValueKind);
    }

    [Fact]
    public async Task Summary_DoesNotExposeClinicalValues()
    {
        var (mother, partner) = await factory.CreateLinkedFamilyAsync();
        var pregnancyId = await CreatePregnancyAsync(mother, daysSinceLastPeriod: 170);
        using var motherClient = factory.CreateClient(mother.AccessToken);
        Assert.Equal(HttpStatusCode.Created, (await motherClient.PostAsJsonAsync(
            "/api/telemetry",
            new { fetalHeartRateBpm = 173, temperatureCelsius = 37.4m })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await motherClient.PostAsJsonAsync(
            $"/api/pregnancy/{pregnancyId}/symptoms",
            new { description = "Dolor lumbar privado", severity = 2 })).StatusCode);
        using var partnerClient = factory.CreateClient(partner.AccessToken);

        var response = await partnerClient.GetAsync("/api/companion/summary");
        var raw = await response.Content.ReadAsStringAsync();
        var summary = await ApiTestFactory.ReadJsonAsync(response);

        Assert.Equal(SummaryProperties, summary.EnumerateObject().Select(property => property.Name).ToArray());
        Assert.Equal(2, summary.GetProperty("healthLight").GetInt32());
        Assert.DoesNotContain("173", raw);
        Assert.DoesNotContain("37.4", raw);
        Assert.DoesNotContain("Dolor lumbar privado", raw);
        Assert.DoesNotContain("symptoms", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fetalHeartRate", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("temperature", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Summary_WhenPartnerNotLinked_ReturnsConflict()
    {
        var partner = await factory.RegisterAsync("Partner");
        using var client = factory.CreateClient(partner.AccessToken);

        var response = await client.GetAsync("/api/companion/summary");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(
            "Aún no estás vinculado a una gestante.",
            (await ApiTestFactory.ReadJsonAsync(response)).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task AlertFlow_CreateListRespondAndRespondAgain()
    {
        var (mother, partner) = await factory.CreateLinkedFamilyAsync();
        using var motherClient = factory.CreateClient(mother.AccessToken);
        using var partnerClient = factory.CreateClient(partner.AccessToken);

        var tiredResponse = await motherClient.PostAsJsonAsync(
            "/api/alerts",
            new { type = 2, message = "Siento mucho cansancio, ¿me ayudas con la cena?" });
        Assert.Equal(HttpStatusCode.Created, tiredResponse.StatusCode);
        var tired = await ApiTestFactory.ReadJsonAsync(tiredResponse);
        Assert.Equal(1, tired.GetProperty("status").GetInt32());
        Assert.Equal(JsonValueKind.Null, tired.GetProperty("response").ValueKind);
        Assert.Equal(JsonValueKind.Null, tired.GetProperty("respondedAt").ValueKind);

        var comeHere = await ApiTestFactory.ReadJsonAsync(
            await motherClient.PostAsJsonAsync("/api/alerts", new { type = 3, message = "  " }));
        Assert.Equal(QuickAlert.DefaultMessage(QuickAlertType.ComeHere), comeHere.GetProperty("message").GetString());

        var pending = (await ApiTestFactory.ReadJsonAsync(await partnerClient.GetAsync("/api/alerts?status=pending")))
            .EnumerateArray().ToArray();
        Assert.Equal(2, pending.Length);
        Assert.Equal(comeHere.GetProperty("id").GetGuid(), pending[0].GetProperty("id").GetGuid());

        var tiredId = tired.GetProperty("id").GetGuid();
        var answer = await partnerClient.PostAsJsonAsync($"/api/alerts/{tiredId}/respond", new { response = 1 });
        Assert.Equal(HttpStatusCode.OK, answer.StatusCode);
        var answered = await ApiTestFactory.ReadJsonAsync(answer);
        Assert.Equal(2, answered.GetProperty("status").GetInt32());
        Assert.Equal(1, answered.GetProperty("response").GetInt32());
        Assert.Equal(JsonValueKind.String, answered.GetProperty("respondedAt").ValueKind);

        var again = await partnerClient.PostAsJsonAsync($"/api/alerts/{tiredId}/respond", new { response = 3 });
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);

        Assert.Single((await ApiTestFactory.ReadJsonAsync(await partnerClient.GetAsync("/api/alerts?status=pending")))
            .EnumerateArray());
        var all = (await ApiTestFactory.ReadJsonAsync(await motherClient.GetAsync("/api/alerts")))
            .EnumerateArray().ToArray();
        Assert.Equal(2, all.Length);
        Assert.Contains(all, alert => alert.GetProperty("id").GetGuid() == tiredId
            && alert.GetProperty("response").GetInt32() == 1);
    }

    [Fact]
    public async Task CreateAlert_WithoutLinkedPartner_ReturnsConflict()
    {
        var mother = await factory.RegisterAsync("Mother");
        using var client = factory.CreateClient(mother.AccessToken);

        var response = await client.PostAsJsonAsync("/api/alerts", new { type = 1 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateAlert_WithUnknownType_ReturnsBadRequest()
    {
        var (mother, _) = await factory.CreateLinkedFamilyAsync();
        using var client = factory.CreateClient(mother.AccessToken);

        var response = await client.PostAsJsonAsync("/api/alerts", new { type = 9 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAlerts_WithInvalidStatus_ReturnsBadRequest()
    {
        var (mother, _) = await factory.CreateLinkedFamilyAsync();
        using var client = factory.CreateClient(mother.AccessToken);

        var response = await client.GetAsync("/api/alerts?status=archived");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Respond_ToAlertFromAnotherFamily_ReturnsNotFound()
    {
        var (mother, _) = await factory.CreateLinkedFamilyAsync();
        var (_, otherPartner) = await factory.CreateLinkedFamilyAsync();
        using var motherClient = factory.CreateClient(mother.AccessToken);
        var alert = await ApiTestFactory.ReadJsonAsync(await motherClient.PostAsJsonAsync("/api/alerts", new { type = 1 }));
        using var otherClient = factory.CreateClient(otherPartner.AccessToken);

        var response = await otherClient.PostAsJsonAsync(
            $"/api/alerts/{alert.GetProperty("id").GetGuid()}/respond",
            new { response = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TipsToday_ReturnsThreeTipsForCurrentWeekAndDoneIsIdempotent()
    {
        var (mother, partner) = await factory.CreateLinkedFamilyAsync();
        await CreatePregnancyAsync(mother, daysSinceLastPeriod: 170);
        using var client = factory.CreateClient(partner.AccessToken);

        var tips = (await ApiTestFactory.ReadJsonAsync(await client.GetAsync("/api/companion/tips/today")))
            .EnumerateArray().ToArray();
        Assert.Equal(3, tips.Length);
        Assert.Equal(3, tips.Select(tip => tip.GetProperty("id").GetGuid()).Distinct().Count());
        Assert.All(tips, tip => Assert.False(tip.GetProperty("done").GetBoolean()));
        Assert.All(tips, tip => Assert.True(
            DailyTipSeed.Tips.Single(seed => seed.Id == tip.GetProperty("id").GetGuid()).AppliesTo(24)));

        var tipId = tips[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/companion/tips/{tipId}/done", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/companion/tips/{tipId}/done", null)).StatusCode);

        var refreshed = (await ApiTestFactory.ReadJsonAsync(await client.GetAsync("/api/companion/tips/today")))
            .EnumerateArray().ToArray();
        Assert.True(refreshed.Single(tip => tip.GetProperty("id").GetGuid() == tipId).GetProperty("done").GetBoolean());
        Assert.Equal(2, refreshed.Count(tip => !tip.GetProperty("done").GetBoolean()));

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/companion/tips/{tipId}/done")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/companion/tips/{tipId}/done")).StatusCode);

        var undone = (await ApiTestFactory.ReadJsonAsync(await client.GetAsync("/api/companion/tips/today")))
            .EnumerateArray().ToArray();
        Assert.All(undone, tip => Assert.False(tip.GetProperty("done").GetBoolean()));
    }

    [Fact]
    public async Task UnmarkTipDone_WithUnknownTip_ReturnsNotFound()
    {
        var partner = await factory.RegisterAsync("Partner");
        using var client = factory.CreateClient(partner.AccessToken);

        var response = await client.DeleteAsync($"/api/companion/tips/{Guid.NewGuid()}/done");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MarkTipDone_WithUnknownTip_ReturnsNotFound()
    {
        var partner = await factory.RegisterAsync("Partner");
        using var client = factory.CreateClient(partner.AccessToken);

        var response = await client.PostAsync($"/api/companion/tips/{Guid.NewGuid()}/done", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void TipSeed_HasAtLeastThirtyTipsAndThreePerWeek()
    {
        Assert.True(DailyTipSeed.Tips.Count >= 30);
        Assert.Equal(DailyTipSeed.Tips.Count, DailyTipSeed.Tips.Select(tip => tip.Id).Distinct().Count());
        for (var week = DailyTip.FirstWeek; week <= DailyTip.LastWeek; week++)
        {
            Assert.True(DailyTipSeed.Tips.Count(tip => tip.AppliesTo(week)) >= 3, $"Week {week} has fewer than 3 tips.");
        }
    }

    [Fact]
    public void SelectForDay_RotatesBetweenConsecutiveDays()
    {
        var candidates = DailyTipSeed.Tips.Where(tip => tip.AppliesTo(24)).ToArray();
        var monday = GetTodayTipsHandler.SelectForDay(candidates, new DateOnly(2026, 11, 2));
        var tuesday = GetTodayTipsHandler.SelectForDay(candidates, new DateOnly(2026, 11, 3));

        Assert.Equal(3, monday.Count);
        Assert.Equal(3, monday.Distinct().Count());
        Assert.NotEqual(monday.Select(tip => tip.Id), tuesday.Select(tip => tip.Id));
    }

    private async Task<Guid> CreatePregnancyAsync(TestAccount mother, int daysSinceLastPeriod)
    {
        var lastPeriod = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-daysSinceLastPeriod);
        using var client = factory.CreateClient(mother.AccessToken);
        var response = await client.PostAsJsonAsync("/api/pregnancy", new
        {
            lastMenstrualPeriod = lastPeriod,
            dueDate = lastPeriod.AddDays(280)
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ApiTestFactory.ReadJsonAsync(response)).GetProperty("id").GetGuid();
    }
}
