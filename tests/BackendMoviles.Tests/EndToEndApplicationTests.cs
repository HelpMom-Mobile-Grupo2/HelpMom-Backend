using BackendMoviles.Application;
using BackendMoviles.Application.Identity;
using BackendMoviles.Application.Pregnancy;
using BackendMoviles.Application.Telemetry;
using BackendMoviles.Application.Triage;
using BackendMoviles.Domain.Identity;
using BackendMoviles.Domain.Pregnancy;
using BackendMoviles.Domain.Telemetry;
using BackendMoviles.Domain.Triage;
using BackendMoviles.Infrastructure;
using BackendMoviles.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BackendMoviles.Tests;

public sealed class EndToEndApplicationTests
{
    [Fact]
    public async Task ApplicationHandlersPersistAllBoundedContexts()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Name"] = $"HelpMomTests-{Guid.NewGuid():N}",
                ["Jwt:Secret"] = "local-test-secret-that-is-at-least-32-bytes-long",
                ["Jwt:Issuer"] = "HelpMomTests",
                ["Jwt:Audience"] = "HelpMomTestsClient"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);
        await using var provider = services.BuildServiceProvider();

        var registration = await SendAsync(provider, new RegisterUserCommand(
            "mother@example.test",
            "Test Mother",
            "a-strong-test-password"));
        Assert.Equal("mother@example.test", registration.User.Email);
        Assert.False(string.IsNullOrWhiteSpace(registration.AccessToken));

        var login = await SendAsync(provider, new LoginCommand("mother@example.test", "a-strong-test-password"));
        Assert.Equal(registration.User.Id, login.User.Id);

        var doctor = await SendAsync(provider, new RegisterUserCommand(
            "doctor@example.test",
            "Test Doctor",
            "another-strong-password"));
        var family = await SendAsync(provider, new AddFamilyMemberCommand(
            registration.FamilyGroupId!.Value,
            doctor.User.Id,
            FamilyRole.Doctor));
        Assert.Contains(family.Members, member => member.Role == FamilyRole.Doctor);

        var pregnancy = await SendAsync(provider, new CreatePregnancyRecordCommand(
            registration.User.Id,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 10, 8)));
        var symptom = await SendAsync(provider, new RecordSymptomCommand(
            pregnancy.Id,
            "Nausea",
            SymptomSeverity.Mild));
        Assert.Equal(SymptomSeverity.Mild, symptom.Severity);
        var updatedPregnancy = await SendAsync(provider, new UpdatePregnancyDatesCommand(
            pregnancy.Id,
            new DateOnly(2026, 1, 2),
            new DateOnly(2026, 10, 9)));
        Assert.Equal(new DateOnly(2026, 10, 9), updatedPregnancy.DueDate);

        var telemetry = await SendAsync(provider, new AddTelemetryCommand(registration.User.Id, 145, 36.8m));
        Assert.Equal(HealthLight.Green, telemetry.HealthLight);
        Assert.Equal(telemetry.Id, (await SendAsync(provider, new GetHealthStatusQuery(registration.User.Id)))?.Id);
        Assert.Single(await SendAsync(provider, new GetTelemetryHistoryQuery(registration.User.Id)));

        var triage = await SendAsync(provider, new SendTriageMessageCommand(registration.User.Id, "Tengo náuseas leves"));
        Assert.Equal(TriageUrgency.SelfCare, triage.Urgency);
        Assert.Equal(2, triage.Messages.Count);
        var followUp = await SendAsync(provider, new SendTriageMessageCommand(
            registration.User.Id,
            "Ahora tengo fiebre",
            triage.Id));
        Assert.Equal(TriageUrgency.ContactCareTeam, followUp.Urgency);
        Assert.Equal(4, followUp.Messages.Count);

        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HelpMomDbContext>();
        var persistedPregnancy = await context.PregnancyRecords
            .Include(record => record.Symptoms)
            .SingleAsync(record => record.Id == pregnancy.Id);
        var persistedConversation = await context.TriageConversations
            .Include(conversation => conversation.Messages)
            .SingleAsync(conversation => conversation.Id == triage.Id);

        Assert.Single(persistedPregnancy.Symptoms);
        Assert.Equal(4, persistedConversation.Messages.Count);
        Assert.Equal(TriageUrgency.ContactCareTeam, persistedConversation.LatestAssessment?.Urgency);
    }

    private static async Task<TResponse> SendAsync<TResponse>(IServiceProvider provider, IRequest<TResponse> request)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }
}