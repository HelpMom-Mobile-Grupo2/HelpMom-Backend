using BackendMoviles.Application.Common;
using BackendMoviles.Domain.Common;
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
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var databaseName = configuration["Database:Name"];
        services.AddDbContext<HelpMomDbContext>(options =>
            options.UseInMemoryDatabase(string.IsNullOrWhiteSpace(databaseName) ? "HelpMomDb" : databaseName));

        services.Configure<JwtTokenOptions>(configuration.GetSection("Jwt"));
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<HelpMomDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IFamilyGroupRepository, FamilyGroupRepository>();
        services.AddScoped<IPregnancyRepository, PregnancyRepository>();
        services.AddScoped<IHealthTelemetryRepository, HealthTelemetryRepository>();
        services.AddScoped<ITriageConversationRepository, TriageConversationRepository>();
        services.AddSingleton<IPasswordService, Pbkdf2PasswordService>();
        services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();
        return services;
    }
}