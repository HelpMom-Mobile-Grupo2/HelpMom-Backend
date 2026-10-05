using BackendMoviles.Domain.Common;

namespace BackendMoviles.Domain.Telemetry;

public sealed class HealthTelemetry : Entity
{
    private HealthTelemetry()
    {
    }

    private HealthTelemetry(
        Guid id,
        Guid motherUserId,
        FetalHeartRate fetalHeartRate,
        BodyTemperature temperature,
        DateTimeOffset measuredAt)
        : base(id)
    {
        MotherUserId = motherUserId;
        FetalHeartRate = fetalHeartRate;
        Temperature = temperature;
        MeasuredAt = measuredAt;
    }

    public Guid MotherUserId { get; private set; }
    public FetalHeartRate FetalHeartRate { get; private set; }
    public BodyTemperature Temperature { get; private set; }
    public DateTimeOffset MeasuredAt { get; private set; }
    public HealthSemaphore Semaphore => HealthSemaphore.Evaluate(FetalHeartRate, Temperature);

    public static HealthTelemetry Create(
        Guid motherUserId,
        FetalHeartRate fetalHeartRate,
        BodyTemperature temperature,
        DateTimeOffset measuredAt)
    {
        if (motherUserId == Guid.Empty)
        {
            throw new ArgumentException("Mother user id cannot be empty.", nameof(motherUserId));
        }

        return new HealthTelemetry(Guid.NewGuid(), motherUserId, fetalHeartRate, temperature, measuredAt);
    }
}