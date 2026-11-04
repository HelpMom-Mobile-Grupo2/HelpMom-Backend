using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BackendMoviles.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BackendMoviles.Tests;

public class ApiTestFactory : WebApplicationFactory<Program>
{
    public const string Password = "a-strong-test-password";

    private readonly string _databaseName = $"HelpMomApiTests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:Secret", "local-test-secret-that-is-at-least-32-bytes-long");
        builder.UseSetting("Jwt:Issuer", "HelpMomTests");
        builder.UseSetting("Jwt:Audience", "HelpMomTestsClient");
        builder.UseSetting("Database:Name", _databaseName);
        builder.ConfigureServices(services => services.AddTransient<IStartupFilter, StreamResponseBodyStartupFilter>());
    }

    public HttpClient CreateClient(string accessToken)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    public async Task<TestAccount> RegisterAsync(string role = "Mother", string? fullName = null)
    {
        var name = fullName ?? $"Test {role}";
        var email = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@example.test";
        using var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new { email, fullName = name, password = Password, role });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await ReadJsonAsync(response);
        var familyGroup = body.GetProperty("familyGroupId");
        return new TestAccount(
            body.GetProperty("user").GetProperty("id").GetGuid(),
            email,
            name,
            familyGroup.ValueKind == JsonValueKind.Null ? null : familyGroup.GetGuid(),
            body.GetProperty("accessToken").GetString()!);
    }

    public async Task<string> CreateInvitationAsync(TestAccount mother)
    {
        using var client = CreateClient(mother.AccessToken);
        var response = await client.PostAsync("/api/family/invitations", null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadJsonAsync(response)).GetProperty("code").GetString()!;
    }

    public async Task<HttpResponseMessage> JoinAsync(TestAccount partner, string code)
    {
        using var client = CreateClient(partner.AccessToken);
        return await client.PostAsJsonAsync("/api/family/join", new { code });
    }

    public async Task<(TestAccount Mother, TestAccount Partner)> CreateLinkedFamilyAsync()
    {
        var mother = await RegisterAsync("Mother", "Daniela Gómez");
        var partner = await RegisterAsync("Partner", "Carlos Ruiz");
        var response = await JoinAsync(partner, await CreateInvitationAsync(mother));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (mother, partner);
    }

    public async Task WithDbContextAsync(Func<HelpMomDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<HelpMomDbContext>());
    }

    public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        return document.RootElement.Clone();
    }
}

// TestServer 8.x exposes a response PipeWriter without UnflushedBytes, which System.Text.Json 9 requires
// when the tests roll forward to the .NET 9 runtime. A stream-backed body feature works on both runtimes.
internal sealed class StreamResponseBodyStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, nextMiddleware) =>
        {
            var original = context.Features.GetRequiredFeature<IHttpResponseBodyFeature>();
            var replacement = new StreamResponseBodyFeature(original.Stream, original);
            context.Features.Set<IHttpResponseBodyFeature>(replacement);
            try
            {
                await nextMiddleware(context);
                await replacement.CompleteAsync();
            }
            finally
            {
                context.Features.Set(original);
            }
        });
        next(app);
    };
}

public sealed record TestAccount(Guid UserId, string Email, string FullName, Guid? FamilyGroupId, string AccessToken);
