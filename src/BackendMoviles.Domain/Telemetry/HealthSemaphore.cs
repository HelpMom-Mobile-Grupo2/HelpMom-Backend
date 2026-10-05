namespace BackendMoviles.Domain.Telemetry;

public enum HealthLight
{
    Green = 1,
    Yellow = 2,
    Red = 3
}

public readonly record struct HealthSemaphore(HealthLight Light, string Summary)
{
    public static HealthSemaphore Evaluate(FetalHeartRate heartRate, BodyTemperature temperature)
    {
        if (heartRate.BeatsPerMinute is < 100 or > 180 || temperature.Celsius >= 39.5m || temperature.Celsius < 35m)
        {
            return new HealthSemaphore(HealthLight.Red, "Valores fuera del rango de observacion; busca atencion medica.");
        }

        if (heartRate.BeatsPerMinute is < 110 or > 160 || temperature.Celsius >= 38m || temperature.Celsius < 36m)
        {
            return new HealthSemaphore(HealthLight.Yellow, "Se recomienda vigilar estos valores y consultar al equipo medico.");
        }

        return new HealthSemaphore(HealthLight.Green, "Valores dentro del rango de referencia simulado.");
    }
}