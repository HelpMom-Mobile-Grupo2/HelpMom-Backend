using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using BackendMoviles.Application.Companion;
using BackendMoviles.Domain.Companion;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BackendMoviles.Tests;

public sealed class EmergencyPolicyFailureTests(FailingAlertsApiTestFactory factory)
    : IClassFixture<FailingAlertsApiTestFactory>
{
    [Fact]
    public async Task RedTelemetry_WhenAlertCreationFails_StillReturnsCreatedAndLogsError()
    {
        var (mother, _) = await factory.CreateLinkedFamilyAsync();
        using var client = factory.CreateClient(mother.AccessToken);

        var response = await client.PostAsJsonAsync("/api/telemetry", new { fetalHeartRateBpm = 190, temperatureCelsius = 36.8m });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var status = await ApiTestFactory.ReadJsonAsync(await client.GetAsync("/api/telemetry/status"));
        Assert.Equal(190, status.GetProperty("fetalHeartRateBpm").GetInt32());
        Assert.Contains(factory.Logs, entry => entry.Level == LogLevel.Error
            && entry.Category == typeof(EmergencyAlertPolicy).FullName
            && entry.Message.Contains(mother.UserId.ToString())
            && entry.Exception is InvalidOperationException);
    }

    [Fact]
    public async Task TriageEmergency_WhenAlertCreationFails_StillReturnsConversation()
    {
        var (mother, _) = await factory.CreateLinkedFamilyAsync();
        using var client = factory.CreateClient(mother.AccessToken);

        var started = await client.PostAsJsonAsync("/api/triage", new { content = "Me desmayé" });
        Assert.Equal(HttpStatusCode.Created, started.StatusCode);
        var conversation = await ApiTestFactory.ReadJsonAsync(started);
        Assert.Equal(3, conversation.GetProperty("urgency").GetInt32());
        var id = conversation.GetProperty("id").GetGuid();

        var followUp = await client.PostAsJsonAsync($"/api/triage/{id}/messages", new { content = "No siento al bebé" });

        Assert.Equal(HttpStatusCode.OK, followUp.StatusCode);
        var stored = await ApiTestFactory.ReadJsonAsync(await client.GetAsync($"/api/triage/{id}"));
        Assert.Equal(4, stored.GetProperty("messages").GetArrayLength());
        Assert.Contains(factory.Logs, entry => entry.Level == LogLevel.Error
            && entry.Message.Contains(EmergencyReasons.TriageEmergencyGeneric));
    }
}

public sealed class FailingAlertsApiTestFactory : ApiTestFactory
{
    public ConcurrentQueue<CapturedLog> Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.AddScoped<IQuickAlertRepository, ThrowingQuickAlertRepository>();
            services.AddSingleton<ILoggerProvider>(new CapturingLoggerProvider(Logs));
        });
    }

    private sealed class ThrowingQuickAlertRepository : IQuickAlertRepository
    {
        private static InvalidOperationException Failure() => new("Simulated quick alert store failure.");

        public Task<QuickAlert?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw Failure();

        public Task<IReadOnlyCollection<QuickAlert>> GetByFamilyAsync(
            Guid familyGroupId,
            bool pendingOnly,
            CancellationToken cancellationToken = default) => throw Failure();

        public Task<bool> HasPendingSinceAsync(
            Guid familyGroupId,
            QuickAlertType type,
            string message,
            DateTimeOffset since,
            CancellationToken cancellationToken = default) => throw Failure();

        public Task AddAsync(QuickAlert alert, CancellationToken cancellationToken = default) => throw Failure();
    }
}

public sealed record CapturedLog(string Category, LogLevel Level, string Message, Exception? Exception);

internal sealed class CapturingLoggerProvider(ConcurrentQueue<CapturedLog> logs) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, logs);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(string category, ConcurrentQueue<CapturedLog> logs) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                logs.Enqueue(new CapturedLog(category, logLevel, formatter(state, exception), exception));
            }
        }
    }
}
