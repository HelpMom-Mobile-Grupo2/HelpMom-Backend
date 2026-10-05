using BackendMoviles.Domain.Common;

namespace BackendMoviles.Domain.Pregnancy;

public sealed class PregnancyRecord : Entity
{
    private readonly List<SymptomEntry> _symptoms = [];

    private PregnancyRecord()
    {
    }

    private PregnancyRecord(Guid id, Guid motherUserId, DateOnly lastMenstrualPeriod, DateOnly dueDate)
        : base(id)
    {
        MotherUserId = motherUserId;
        LastMenstrualPeriod = lastMenstrualPeriod;
        DueDate = dueDate;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid MotherUserId { get; private set; }
    public DateOnly LastMenstrualPeriod { get; private set; }
    public DateOnly DueDate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<SymptomEntry> Symptoms => _symptoms.AsReadOnly();

    public static PregnancyRecord Create(Guid motherUserId, DateOnly lastMenstrualPeriod, DateOnly dueDate)
    {
        if (motherUserId == Guid.Empty)
        {
            throw new ArgumentException("Mother user id cannot be empty.", nameof(motherUserId));
        }

        if (dueDate <= lastMenstrualPeriod || dueDate.DayNumber - lastMenstrualPeriod.DayNumber > 308)
        {
            throw new ArgumentException("Due date must be within 44 weeks after the last menstrual period.", nameof(dueDate));
        }

        return new PregnancyRecord(Guid.NewGuid(), motherUserId, lastMenstrualPeriod, dueDate);
    }

    public GestationalAge GetGestationalAge(DateOnly asOfDate) =>
        GestationalAge.FromLastMenstrualPeriod(LastMenstrualPeriod, asOfDate);

    public void UpdateDates(DateOnly lastMenstrualPeriod, DateOnly dueDate)
    {
        if (dueDate <= lastMenstrualPeriod || dueDate.DayNumber - lastMenstrualPeriod.DayNumber > 308)
        {
            throw new ArgumentException("Due date must be within 44 weeks after the last menstrual period.", nameof(dueDate));
        }

        LastMenstrualPeriod = lastMenstrualPeriod;
        DueDate = dueDate;
    }

    public SymptomEntry RecordSymptom(string description, SymptomSeverity severity, DateTimeOffset occurredAt)
    {
        var symptom = SymptomEntry.Create(description, severity, occurredAt);
        _symptoms.Add(symptom);
        return symptom;
    }
}