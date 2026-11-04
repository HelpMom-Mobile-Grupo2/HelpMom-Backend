using BackendMoviles.Application.Common;
using BackendMoviles.Domain.Common;
using BackendMoviles.Domain.Companion;
using BackendMoviles.Domain.Identity;
using BackendMoviles.Domain.Pregnancy;
using BackendMoviles.Domain.Telemetry;
using BackendMoviles.Domain.Triage;
using BackendMoviles.Infrastructure.Persistence;
using BackendMoviles.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BackendMoviles.Infrastructure;

public static class DependencyInjection
{
    public const string DefaultSqliteConnection = "Data Source=helpmom.db";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"];
        if (string.IsNullOrWhiteSpace(provider) || provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            var databaseName = configuration["Database:Name"];
            services.AddDbContext<HelpMomDbContext>(options =>
                options.UseInMemoryDatabase(string.IsNullOrWhiteSpace(databaseName) ? "HelpMomDb" : databaseName));
        }
        else if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration["Database:ConnectionString"];
            services.AddDbContext<HelpMomDbContext>(options =>
                options.UseSqlite(string.IsNullOrWhiteSpace(connectionString) ? DefaultSqliteConnection : connectionString));
        }
        else if (provider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = GetPostgreSqlConnectionString(configuration);
            services.AddDbContext<HelpMomDbContext, PostgreSqlHelpMomDbContext>(options =>
                options.UseNpgsql(connectionString));
        }
        else
        {
            throw new InvalidOperationException("Database:Provider must be Sqlite, PostgreSql or InMemory.");
        }

        services.Configure<JwtTokenOptions>(configuration.GetSection("Jwt"));
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<HelpMomDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IFamilyGroupRepository, FamilyGroupRepository>();
        services.AddScoped<IFamilyInvitationRepository, FamilyInvitationRepository>();
        services.AddScoped<IPregnancyRepository, PregnancyRepository>();
        services.AddScoped<IHealthTelemetryRepository, HealthTelemetryRepository>();
        services.AddScoped<ITriageConversationRepository, TriageConversationRepository>();
        services.AddScoped<IQuickAlertRepository, QuickAlertRepository>();
        services.AddScoped<IDailyTipRepository, DailyTipRepository>();
        services.AddSingleton<IPasswordService, Pbkdf2PasswordService>();
        services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();
        return services;
    }

    // DATABASE_URL (set by Render) wins over Database:ConnectionString, which must already be in Npgsql format.
    private static string GetPostgreSqlConnectionString(IConfiguration configuration)
    {
        var databaseUrl = configuration["DATABASE_URL"];
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return PostgreSqlConnectionString.FromDatabaseUrl(databaseUrl);
        }

        var connectionString = configuration["Database:ConnectionString"];
        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new InvalidOperationException(
                "Database:Provider PostgreSql requires DATABASE_URL or Database:ConnectionString.")
            : connectionString;
    }
}