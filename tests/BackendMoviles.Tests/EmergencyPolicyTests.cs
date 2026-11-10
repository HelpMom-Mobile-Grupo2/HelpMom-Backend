using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BackendMoviles.Application.Companion;
using BackendMoviles.Domain.Companion;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BackendMoviles.Tests;

public sealed class EmergencyPolicyTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    private static readonly string RedTelemetryMessage = EmergencyAlertPolicy.MessageFor(EmergencyReasons.RedTelemetry);
    private static readonly string TriageMessage = EmergencyAlertPolicy.MessageFor(
        EmergencyReasons.TriageEmergency("posible ruptura de fuente"));

    [Fact]
    public async Task TriageEmergency_CreatesSosAlertForPartner()
    {
        var (mother, partner) = await factory.CreateLinkedFamilyAsync();
        using var motherClient = factory.CreateClient(mother.AccessToken);

        var triage = await ApiTestFactory.ReadJsonAsync(
            await motherClient.PostAsJsonAsync("/api/triage", new { content = "Creo que rompí fuente" }));

        Assert.Equal(3, triage.GetProperty("urgency").GetInt32());
        var alert = Assert.Single(await GetPartnerAlertsAsync(partner));
        Assert.Equal((int)QuickAlertType.Sos, alert.GetProperty("type").GetInt32());
        Assert.Equal(1, alert.GetProperty("status").GetInt32());
        Assert.Equal(TriageMessage, alert.GetProperty("message").GetString());
    }

    [Fact]
    public async Task NonEmergencyTriage_DoesNotCreateAlert()
    {
        var (mother, partner) = await factory.CreateLinkedFamilyAsync();
        using var motherClient = factory.CreateClient(mother.AccessToken);

        await motherClient.PostAsJsonAsync("/api/triage", new { content = "Tengo un poco de fiebre" });

        Assert.Empty(await GetPartnerAlertsAsync(partner));
    }

    [Fact]
    public async Task RedTelemetry_CreatesSosAlertForPartner()
    {
        var (mother, partner) = await factory.CreateLinkedFamilyAsync();

        await PostMeasurementAsync(mother, fetalHeartRateBpm: 190);

        var alert = Assert.Single(await GetPartnerAlertsAsync(partner));
        Assert.Equal((int)QuickAlertType.Sos, alert.GetProperty("type").GetInt32());
        Assert.Equal(RedTelemetryMessage, alert.GetProperty("message").GetString());
    }

    [Fact]
    public async Task GreenOrYellowTelemetry_DoesNotCreateAlert()
    {
        var (mother, partner) = await factory.CreateLinkedFamilyAsync();

        await PostMeasurementAsync(mother, fetalHeartRateBpm: 140);
        await PostMeasurementAsync(mother, fetalHeartRateBpm: 170);

        Assert.Empty(await GetPartnerAlertsAsync(partner));
    }

    [Fact]
    public async Task RepeatedRedTelemetry_WithinTenMinutes_CreatesSingleSos()
    {
        var (mother, partner) = await factory.CreateLinkedFamilyAsync();

        await PostMeasurementAsync(mother, fetalHeartRateBpm: 190);
        await PostMeasurementAsync(mother, fetalHeartRateBpm: 195);
        await PostMeasurementAsync(mother, fetalHeartRateBpm: 140, temperatureCelsius: 39.8m);

        Assert.Single(await GetPartnerAlertsAsync(partner));
    }

    [Fact]
    public async Task RedTelemetry_AfterTenMinutes_CreatesNewSos()
    {
        var (mother, partner) = await factory.CreateLinkedFamilyAsync();
        await PostMeasurementAsync(mother, fetalHeartRateBpm: 190);
        await factory.WithDbContextAsync(async context =>
        {
            var alert = await context.QuickAlerts.SingleAsync(item => item.FamilyGroupId == mother.FamilyGroupId);
            context.Entry(alert).Property(item => item.CreatedAt).CurrentValue =
                DateTimeOffset.UtcNow - EmergencyAlertPolicy.DuplicateWindow - TimeSpan.FromMinutes(1);
            await context.SaveChangesAsync();
        });

        await PostMeasurementAsync(mother, fetalHeartRateBpm: 190);

        Assert.Equal(2, (await GetPartnerAlertsAsync(partner)).Length);
    }

    [Fact]
    public async Task RedTelemetry_AfterPartnerResponded_CreatesNewSos()
    {
        var (mother, partner) = await factory.CreateLinkedFamilyAsync();
        await PostMeasurementAsync(mother, fetalHeartRateBpm: 190);
        var first = Assert.Single(await GetPartnerAlertsAsync(partner));
        using var partnerClient = factory.CreateClient(partner.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await partnerClient.PostAsJsonAsync(
            $"/api/alerts/{first.GetProperty("id").GetGuid()}/respond",
            new { response = 2 })).StatusCode);

        await PostMeasurementAsync(mother, fetalHeartRateBpm: 190);

        Assert.Equal(2, (await GetPartnerAlertsAsync(partner)).Length);
    }

    [Fact]
    public async Task RedTelemetryAndTriageEmergency_CreateOneSosPerAlarmSign()
    {
        var (mother, partner) = await factory.CreateLinkedFamilyAsync();
        using var motherClient = factory.CreateClient(mother.AccessToken);

        await PostMeasurementAsync(mother, fetalHeartRateBpm: 190);
        await motherClient.PostAsJsonAsync("/api/triage", new { content = "Me desmayé hace un rato" });
        await motherClient.PostAsJsonAsync("/api/triage", new { content = "No siento al bebé" });

        var messages = (await GetPartnerAlertsAsync(partner)).Select(alert => alert.GetProperty("message").GetString());
        Assert.Equal(
        [
            EmergencyAlertPolicy.MessageFor(EmergencyReasons.TriageEmergency("el bebé se mueve menos")),
            EmergencyAlertPolicy.MessageFor(EmergencyReasons.TriageEmergency("desmayo o pérdida de conciencia")),
            RedTelemetryMessage,
        ], messages);
    }

    [Fact]
    public async Task RedTelemetry_WithoutPartner_StillSavesMeasurement()
    {
        var mother = await factory.RegisterAsync("Mother");

        await PostMeasurementAsync(mother, fetalHeartRateBpm: 190);

        await factory.WithDbContextAsync(async context =>
            Assert.False(await context.QuickAlerts.AnyAsync(alert => alert.FamilyGroupId == mother.FamilyGroupId)));
    }

    [Theory]
    [InlineData("Me estoy desangrando")]
    [InlineData("Rompí fuente")]
    [InlineData("creo que rompi fuente")]
    [InlineData("No siento al bebé desde la mañana")]
    [InlineData("no siento al bebe")]
    [InlineData("Tengo un DOLOR MUY FUERTE en el vientre")]
    [InlineData("Tengo visión borrosa")]
    [InlineData("vision borrosa y mareo")]
    [InlineData("Me desmayé")]
    [InlineData("me desmaye en la cocina")]
    [InlineData("tengo sangrado y mucho dolor")]
    [InlineData("estoy sangrando muchísimo")]
    [InlineData("me cuesta respirar")]
    [InlineData("mi bebé no se mueve")]
    [InlineData("me duele mucho la cabeza")]
    [InlineData("tengo fiebre muy alta")]
    [InlineData("se me rompió la fuente")]
    public async Task Triage_ColloquialEmergencyTerms_AreEmergency(string content)
    {
        var mother = await factory.RegisterAsync("Mother");
        using var client = factory.CreateClient(mother.AccessToken);

        var triage = await ApiTestFactory.ReadJsonAsync(await client.PostAsJsonAsync("/api/triage", new { content }));

        Assert.Equal(3, triage.GetProperty("urgency").GetInt32());
    }

    [Theory]
    [InlineData("estoy sangrando mucho", "sangrado abundante")]
    [InlineData("me falta el aire", "dificultad para respirar")]
    [InlineData("siento un dolor insoportable", "dolor muy intenso")]
    public async Task TriageEmergency_SosMessageNamesTheAlarmSign(string content, string expectedSign)
    {
        var (mother, partner) = await factory.CreateLinkedFamilyAsync();
        using var motherClient = factory.CreateClient(mother.AccessToken);

        await motherClient.PostAsJsonAsync("/api/triage", new { content });

        var alert = Assert.Single(await GetPartnerAlertsAsync(partner));
        Assert.Contains(expectedSign, alert.GetProperty("message").GetString());
    }

    [Theory]
    [InlineData("Tengo náuseas leves", 1)]
    [InlineData("tengo un poco de dolor de espalda", 2)]
    [InlineData("me duele un poco la cabeza", 2)]
    public async Task Triage_NonEmergencyPhrases_KeepTheirLevel(string content, int expectedUrgency)
    {
        var mother = await factory.RegisterAsync("Mother");
        using var client = factory.CreateClient(mother.AccessToken);

        var triage = await ApiTestFactory.ReadJsonAsync(await client.PostAsJsonAsync("/api/triage", new { content }));

        Assert.Equal(expectedUrgency, triage.GetProperty("urgency").GetInt32());
    }

    private async Task PostMeasurementAsync(TestAccount mother, int fetalHeartRateBpm, decimal temperatureCelsius = 36.8m)
    {
        using var client = factory.CreateClient(mother.AccessToken);
        var response = await client.PostAsJsonAsync("/api/telemetry", new { fetalHeartRateBpm, temperatureCelsius });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<JsonElement[]> GetPartnerAlertsAsync(TestAccount partner)
    {
        using var client = factory.CreateClient(partner.AccessToken);
        var response = await client.GetAsync("/api/alerts?status=all");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await ApiTestFactory.ReadJsonAsync(response)).EnumerateArray().ToArray();
    }
}
