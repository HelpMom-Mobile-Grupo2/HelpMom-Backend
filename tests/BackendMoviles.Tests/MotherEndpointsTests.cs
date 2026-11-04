using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace BackendMoviles.Tests;

public sealed class MotherEndpointsTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    [Fact]
    public async Task TelemetryHistory_ReturnsMostRecentFirstLimitedByCount()
    {
        var mother = await factory.RegisterAsync("Mother");
        using var client = factory.CreateClient(mother.AccessToken);
        var start = DateTimeOffset.UtcNow.AddHours(-3);
        for (var hour = 0; hour < 3; hour++)
        {
            await client.PostAsJsonAsync("/api/telemetry", new
            {
                fetalHeartRateBpm = 140 + hour,
                temperatureCelsius = 36.8m,
                measuredAt = start.AddHours(hour)
            });
        }

        var response = await client.GetAsync("/api/telemetry/history?count=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var history = (await ApiTestFactory.ReadJsonAsync(response)).EnumerateArray().ToArray();
        Assert.Equal([142, 141], history.Select(item => item.GetProperty("fetalHeartRateBpm").GetInt32()));
        Assert.Equal(3, (await ApiTestFactory.ReadJsonAsync(await client.GetAsync("/api/telemetry/history")))
            .GetArrayLength());
    }

    [Fact]
    public async Task TelemetryHistory_WithInvalidCount_ReturnsBadRequest()
    {
        var mother = await factory.RegisterAsync("Mother");
        using var client = factory.CreateClient(mother.AccessToken);

        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/telemetry/history?count=0")).StatusCode);
    }

    [Fact]
    public async Task Pregnancy_UpdateAndDelete_ByOwner()
    {
        var mother = await factory.RegisterAsync("Mother");
        using var client = factory.CreateClient(mother.AccessToken);
        var id = await CreatePregnancyAsync(client);

        var update = await client.PutAsJsonAsync($"/api/pregnancy/{id}", new
        {
            lastMenstrualPeriod = new DateOnly(2026, 5, 12),
            dueDate = new DateOnly(2027, 2, 16)
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("2027-02-16", (await ApiTestFactory.ReadJsonAsync(update)).GetProperty("dueDate").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/pregnancy/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/pregnancy/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/pregnancy/{id}")).StatusCode);
    }

    [Fact]
    public async Task Pregnancy_UpdateWithInvalidDates_ReturnsBadRequest()
    {
        var mother = await factory.RegisterAsync("Mother");
        using var client = factory.CreateClient(mother.AccessToken);
        var id = await CreatePregnancyAsync(client);

        var response = await client.PutAsJsonAsync($"/api/pregnancy/{id}", new
        {
            lastMenstrualPeriod = new DateOnly(2026, 5, 10),
            dueDate = new DateOnly(2026, 5, 1)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Pregnancy_UpdateAndDelete_ByAnotherMother_ReturnNotFound()
    {
        var owner = await factory.RegisterAsync("Mother");
        using var ownerClient = factory.CreateClient(owner.AccessToken);
        var id = await CreatePregnancyAsync(ownerClient);
        var other = await factory.RegisterAsync("Mother");
        using var otherClient = factory.CreateClient(other.AccessToken);

        var update = await otherClient.PutAsJsonAsync($"/api/pregnancy/{id}", new
        {
            lastMenstrualPeriod = new DateOnly(2026, 5, 12),
            dueDate = new DateOnly(2027, 2, 16)
        });

        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherClient.DeleteAsync($"/api/pregnancy/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ownerClient.GetAsync($"/api/pregnancy/{id}")).StatusCode);
    }

    [Fact]
    public async Task TriageList_ReturnsOwnConversationsNewestFirst()
    {
        var mother = await factory.RegisterAsync("Mother");
        using var client = factory.CreateClient(mother.AccessToken);
        var first = await ApiTestFactory.ReadJsonAsync(
            await client.PostAsJsonAsync("/api/triage", new { content = "Tengo náuseas leves" }));
        var second = await ApiTestFactory.ReadJsonAsync(
            await client.PostAsJsonAsync("/api/triage", new { content = "Tengo fiebre" }));
        var otherMother = await factory.RegisterAsync("Mother");
        using var otherClient = factory.CreateClient(otherMother.AccessToken);
        await otherClient.PostAsJsonAsync("/api/triage", new { content = "Hola" });

        var response = await client.GetAsync("/api/triage");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = (await ApiTestFactory.ReadJsonAsync(response)).EnumerateArray().ToArray();
        Assert.Equal(
            [second.GetProperty("id").GetGuid(), first.GetProperty("id").GetGuid()],
            items.Select(item => item.GetProperty("id").GetGuid()));
        Assert.Equal(2, items[0].GetProperty("urgency").GetInt32());
        Assert.Equal(
            second.GetProperty("guidance").GetString(),
            items[0].GetProperty("lastMessage").GetString());
        Assert.Equal(
            ["id", "createdAt", "urgency", "lastMessage"],
            items[0].EnumerateObject().Select(property => property.Name));
    }

    private static async Task<Guid> CreatePregnancyAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/pregnancy", new
        {
            lastMenstrualPeriod = new DateOnly(2026, 5, 10),
            dueDate = new DateOnly(2027, 2, 14)
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ApiTestFactory.ReadJsonAsync(response)).GetProperty("id").GetGuid();
    }
}
