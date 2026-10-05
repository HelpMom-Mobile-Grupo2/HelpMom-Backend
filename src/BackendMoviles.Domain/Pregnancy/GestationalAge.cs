namespace BackendMoviles.Domain.Pregnancy;

public readonly record struct GestationalAge(int Weeks, int Days)
{
    public static GestationalAge FromLastMenstrualPeriod(DateOnly lastMenstrualPeriod, DateOnly asOfDate)
    {
        var elapsedDays = asOfDate.DayNumber - lastMenstrualPeriod.DayNumber;
        if (elapsedDays < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(asOfDate), "Date cannot precede the last menstrual period.");
        }

        return new GestationalAge(elapsedDays / 7, elapsedDays % 7);
    }
}