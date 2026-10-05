namespace BackendMoviles.Domain.Telemetry;

public interface IHealthTelemetryRepository
{
    Task<IReadOnlyCollection<HealthTelemetry>> GetRecentByMotherAsync(
        Guid motherUserId,
        int count,
        CancellationToken cancellationToken = default);
    Task AddAsync(HealthTelemetry telemetry, CancellationToken cancellationToken = default);
}