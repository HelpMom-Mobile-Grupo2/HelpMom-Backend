using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using BackendMoviles.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BackendMoviles.Tests;

public sealed class FamilyTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
    [Fact]
    public async Task Register_AsPartner_ReturnsPartnerRoleWithoutFamilyGroup()
    {
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"partner-{Guid.NewGuid():N}@example.test",
            fullName = "Carlos Ruiz",
            password = ApiTestFactory.Password,
            role = "Partner"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await ApiTestFactory.ReadJsonAsync(response);
        Assert.Equal("Partner", body.GetProperty("role").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("familyGroupId").ValueKind);
    }

    [Fact]
    public async Task Register_WithoutRole_CreatesMotherWithFamilyGroup()
    {
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"mother-{Guid.NewGuid():N}@example.test",
            fullName = "Daniela Gómez",
            password = ApiTestFactory.Password
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await ApiTestFactory.ReadJsonAsync(response);
        Assert.Equal("Mother", body.GetProperty("role").GetString());
        Assert.Equal(JsonValueKind.String, body.GetProperty("familyGroupId").ValueKind);
    }

    [Fact]
    public async Task Register_AsDoctor_ReturnsBadRequest()
    {
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"doctor-{Guid.NewGuid():N}@example.test",
            fullName = "Doctor",
            password = ApiTestFactory.Password,
            role = "Doctor"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ApiTestFactory.ReadJsonAsync(response);
        Assert.True(body.GetProperty("errors").TryGetProperty("Role", out _));
    }

    [Fact]
    public async Task Login_ReturnsRealFamilyGroupIdAndRole()
    {
        var (mother, partner) = await factory.CreateLinkedFamilyAsync();
        using var client = factory.CreateClient();

        var motherLogin = await ApiTestFactory.ReadJsonAsync(
            await client.PostAsJsonAsync("/api/auth/login", new { email = mother.Email, password = ApiTestFactory.Password }));
        var partnerLogin = await ApiTestFactory.ReadJsonAsync(
            await client.PostAsJsonAsync("/api/auth/login", new { email = partner.Email, password = ApiTestFactory.Password }));

        Assert.Equal(mother.FamilyGroupId, motherLogin.GetProperty("familyGroupId").GetGuid());
        Assert.Equal("Mother", motherLogin.GetProperty("role").GetString());
        Assert.Equal(mother.FamilyGroupId, partnerLogin.GetProperty("familyGroupId").GetGuid());
        Assert.Equal("Partner", partnerLogin.GetProperty("role").GetString());
    }

    [Fact]
    public async Task InvitationAndJoin_LinksPartnerToMother()
    {
        var mother = await factory.RegisterAsync("Mother", "Daniela Gómez");
        var partner = await factory.RegisterAsync("Partner", "Carlos Ruiz");

        using var motherClient = factory.CreateClient(mother.AccessToken);
        var invitationResponse = await motherClient.PostAsync("/api/family/invitations", null);
        Assert.Equal(HttpStatusCode.Created, invitationResponse.StatusCode);
        var invitation = await ApiTestFactory.ReadJsonAsync(invitationResponse);
        var code = invitation.GetProperty("code").GetString()!;
        Assert.Matches(new Regex(@"^HM-\d{4}$"), code);
        var expiresIn = invitation.GetProperty("expiresAt").GetDateTimeOffset() - DateTimeOffset.UtcNow;
        Assert.InRange(expiresIn, TimeSpan.FromHours(23.9), TimeSpan.FromHours(24));

        var joinResponse = await factory.JoinAsync(partner, code);
        Assert.Equal(HttpStatusCode.OK, joinResponse.StatusCode);
        var join = await ApiTestFactory.ReadJsonAsync(joinResponse);
        Assert.Equal(mother.FamilyGroupId, join.GetProperty("familyGroupId").GetGuid());
        Assert.Equal("Daniela Gómez", join.GetProperty("motherName").GetString());

        using var partnerClient = factory.CreateClient(partner.AccessToken);
        var family = await ApiTestFactory.ReadJsonAsync(await partnerClient.GetAsync("/api/family"));
        var members = family.GetProperty("members").EnumerateArray().ToArray();
        Assert.Equal(2, members.Length);
        Assert.Contains(members, member => member.GetProperty("fullName").GetString() == "Daniela Gómez"
            && member.GetProperty("role").GetInt32() == (int)FamilyRole.Mother);
        Assert.Contains(members, member => member.GetProperty("fullName").GetString() == "Carlos Ruiz"
            && member.GetProperty("role").GetInt32() == (int)FamilyRole.Partner);
    }

    [Fact]
    public async Task Join_WithExpiredCode_ReturnsConflict()
    {
        var mother = await factory.RegisterAsync("Mother");
        var partner = await factory.RegisterAsync("Partner");
        var code = await factory.CreateInvitationAsync(mother);
        await factory.WithDbContextAsync(async context =>
        {
            var invitation = await context.FamilyInvitations.SingleAsync(item => item.FamilyGroupId == mother.FamilyGroupId);
            context.Entry(invitation).Property(item => item.ExpiresAt).CurrentValue = DateTimeOffset.UtcNow.AddMinutes(-1);
            await context.SaveChangesAsync();
        });

        var response = await factory.JoinAsync(partner, code);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("venció", (await ApiTestFactory.ReadJsonAsync(response)).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Join_WithUsedCode_ReturnsConflict()
    {
        var mother = await factory.RegisterAsync("Mother");
        var code = await factory.CreateInvitationAsync(mother);
        Assert.Equal(HttpStatusCode.OK, (await factory.JoinAsync(await factory.RegisterAsync("Partner"), code)).StatusCode);

        var response = await factory.JoinAsync(await factory.RegisterAsync("Partner"), code);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("ya fue usado", (await ApiTestFactory.ReadJsonAsync(response)).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Join_WithUnknownCode_ReturnsNotFound()
    {
        var partner = await factory.RegisterAsync("Partner");
        var unusedCode = string.Empty;
        await factory.WithDbContextAsync(async context =>
        {
            var existing = await context.FamilyInvitations.Select(item => item.Code).ToListAsync();
            unusedCode = Enumerable.Range(0, 10_000).Select(number => $"HM-{number:D4}").First(code => !existing.Contains(code));
        });

        var response = await factory.JoinAsync(partner, unusedCode);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Join_WithMalformedCode_ReturnsBadRequest()
    {
        var partner = await factory.RegisterAsync("Partner");

        var response = await factory.JoinAsync(partner, "ABC");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NewInvitation_InvalidatesPreviousOne()
    {
        var mother = await factory.RegisterAsync("Mother");
        var firstCode = await factory.CreateInvitationAsync(mother);
        var secondCode = await factory.CreateInvitationAsync(mother);
        Assert.NotEqual(firstCode, secondCode);

        var response = await factory.JoinAsync(await factory.RegisterAsync("Partner"), firstCode);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await factory.JoinAsync(await factory.RegisterAsync("Partner"), secondCode)).StatusCode);
    }

    [Fact]
    public async Task Join_WhenPartnerAlreadyLinked_ReturnsConflict()
    {
        var (_, partner) = await factory.CreateLinkedFamilyAsync();
        var otherMother = await factory.RegisterAsync("Mother");

        var response = await factory.JoinAsync(partner, await factory.CreateInvitationAsync(otherMother));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("Ya estás vinculado", (await ApiTestFactory.ReadJsonAsync(response)).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Join_WhenMotherAlreadyHasPartner_ReturnsConflict()
    {
        var (mother, _) = await factory.CreateLinkedFamilyAsync();

        var response = await factory.JoinAsync(await factory.RegisterAsync("Partner"), await factory.CreateInvitationAsync(mother));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RemovePartner_UnlinksPartnerFromFamily()
    {
        var (mother, partner) = await factory.CreateLinkedFamilyAsync();
        using var motherClient = factory.CreateClient(mother.AccessToken);
        using var partnerClient = factory.CreateClient(partner.AccessToken);

        Assert.Equal(HttpStatusCode.NoContent, (await motherClient.DeleteAsync("/api/family/partner")).StatusCode);

        var family = await ApiTestFactory.ReadJsonAsync(await motherClient.GetAsync("/api/family"));
        Assert.Single(family.GetProperty("members").EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await partnerClient.GetAsync("/api/family")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await motherClient.DeleteAsync("/api/family/partner")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await factory.JoinAsync(partner, await factory.CreateInvitationAsync(mother))).StatusCode);
    }

    [Theory]
    [InlineData("Partner", "GET", "/api/telemetry/status")]
    [InlineData("Partner", "GET", "/api/pregnancy")]
    [InlineData("Partner", "POST", "/api/family/invitations")]
    [InlineData("Partner", "DELETE", "/api/family/partner")]
    [InlineData("Mother", "POST", "/api/family/join")]
    public async Task Endpoints_RejectWrongRole_WithForbidden(string role, string method, string path)
    {
        var account = await factory.RegisterAsync(role);
        using var client = factory.CreateClient(account.AccessToken);
        using var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = method == "POST" ? JsonContent.Create(new { code = "HM-0000" }) : null
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
