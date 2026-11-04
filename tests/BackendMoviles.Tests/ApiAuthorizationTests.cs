using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BackendMoviles.Tests;

public sealed class ApiAuthorizationTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    private const string AnyId = "00000000-0000-0000-0000-000000000001";

    [Theory]
    [InlineData("Partner", "POST", "/api/telemetry")]
    [InlineData("Partner", "GET", "/api/telemetry/status")]
    [InlineData("Partner", "GET", "/api/telemetry/history")]
    [InlineData("Partner", "GET", "/api/pregnancy")]
    [InlineData("Partner", "PUT", "/api/pregnancy/" + AnyId)]
    [InlineData("Partner", "DELETE", "/api/pregnancy/" + AnyId)]
    [InlineData("Partner", "POST", "/api/triage")]
    [InlineData("Partner", "GET", "/api/triage")]
    [InlineData("Partner", "POST", "/api/family/invitations")]
    [InlineData("Partner", "DELETE", "/api/family/partner")]
    [InlineData("Partner", "POST", "/api/alerts")]
    [InlineData("Mother", "POST", "/api/family/join")]
    [InlineData("Mother", "GET", "/api/companion/summary")]
    [InlineData("Mother", "GET", "/api/companion/tips/today")]
    [InlineData("Mother", "POST", "/api/companion/tips/" + AnyId + "/done")]
    [InlineData("Mother", "POST", "/api/alerts/" + AnyId + "/respond")]
    public async Task Endpoints_RejectWrongRole_WithForbidden(string role, string method, string path)
    {
        var account = await factory.RegisterAsync(role);
        using var client = factory.CreateClient(account.AccessToken);
        using var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = method is "POST" or "PUT" ? JsonContent.Create(new { }) : null
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/family")]
    [InlineData("GET", "/api/alerts")]
    [InlineData("GET", "/api/companion/summary")]
    [InlineData("GET", "/api/telemetry/status")]
    public async Task Endpoints_WithoutToken_ReturnUnauthorized(string method, string path)
    {
        using var client = factory.CreateClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_GroupsEndpointsByModuleTags()
    {
        using var client = factory.CreateClient();

        var document = await ApiTestFactory.ReadJsonAsync(await client.GetAsync("/swagger/v1/swagger.json"));

        var tags = document.GetProperty("paths").EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject())
            .SelectMany(operation => operation.Value.GetProperty("tags").EnumerateArray())
            .Select(tag => tag.GetString()!)
            .Distinct()
            .Order()
            .ToArray();
        Assert.Equal(["Alerts", "Auth", "Companion", "Family", "Pregnancy", "Telemetry", "Triage"], tags);
    }
}
