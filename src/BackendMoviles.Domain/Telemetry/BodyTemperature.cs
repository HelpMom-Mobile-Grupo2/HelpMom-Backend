namespace BackendMoviles.Domain.Telemetry;

public readonly record struct BodyTemperature
{
    private BodyTemperature(decimal celsius) => Celsius = celsius;

    public decimal Celsius { get; }

    public static BodyTemperature Create(decimal celsius)
    {
        if (celsius is < 30m or > 45m)
        {
            throw new ArgumentOutOfRangeException(nameof(celsius), "Temperature must be between 30 and 45 degrees Celsius.");
        }

        return new BodyTemperature(celsius);
    }
}