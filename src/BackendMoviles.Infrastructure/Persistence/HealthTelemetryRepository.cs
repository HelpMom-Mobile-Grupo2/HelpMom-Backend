using BackendMoviles.Domain.Telemetry;
using Microsoft.EntityFrameworkCore;

namespace BackendMoviles.Infrastructure.Persistence;

public sealed class HealthTelemetryRepository(HelpMomDbContext context)
    : Repository<HealthTelemetry>(context), IHealthTelemetryRepository
{
    public async Task<IReadOnlyCollection<HealthTelemetry>> GetRecentByMotherAsync(
        Guid motherUserId,
        int count,
        CancellationToken cancellationToken = default)
    {
        if (count is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be between 1 and 500.");
        }

        return await Entities.Where(measurement => measurement.MotherUserId == motherUserId)
            .OrderByDescending(measurement => measurement.MeasuredAt)
            .Take(count)
            .ToArrayAsync(cancellationToken);
    }
}