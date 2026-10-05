namespace BackendMoviles.Domain.Telemetry;

public readonly record struct FetalHeartRate
{
    private FetalHeartRate(int beatsPerMinute) => BeatsPerMinute = beatsPerMinute;

    public int BeatsPerMinute { get; }

    public static FetalHeartRate Create(int beatsPerMinute)
    {
        if (beatsPerMinute is < 30 or > 250)
        {
            throw new ArgumentOutOfRangeException(nameof(beatsPerMinute), "Heart rate must be between 30 and 250 bpm.");
        }

        return new FetalHeartRate(beatsPerMinute);
    }
}