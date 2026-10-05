using BackendMoviles.Domain.Common;

namespace BackendMoviles.Domain.Pregnancy;

public sealed class SymptomEntry : Entity
{
    private SymptomEntry() => Description = string.Empty;

    internal SymptomEntry(Guid id, string description, SymptomSeverity severity, DateTimeOffset occurredAt)
        : base(id)
    {
        Description = description;
        Severity = severity;
        OccurredAt = occurredAt;
    }

    public string Description { get; private set; }
    public SymptomSeverity Severity { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    internal static SymptomEntry Create(string description, SymptomSeverity severity, DateTimeOffset occurredAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (!Enum.IsDefined(severity))
        {
            throw new ArgumentOutOfRangeException(nameof(severity));
        }

        return new SymptomEntry(Guid.NewGuid(), description.Trim(), severity, occurredAt);
    }
}