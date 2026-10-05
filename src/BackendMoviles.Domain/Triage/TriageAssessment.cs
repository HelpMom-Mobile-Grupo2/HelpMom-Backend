namespace BackendMoviles.Domain.Triage;

public sealed record TriageAssessment(TriageUrgency Urgency, string Guidance, DateTimeOffset AssessedAt)
{
    public static TriageAssessment Create(TriageUrgency urgency, string guidance, DateTimeOffset assessedAt)
    {
        if (!Enum.IsDefined(urgency))
        {
            throw new ArgumentOutOfRangeException(nameof(urgency));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(guidance);
        return new TriageAssessment(urgency, guidance.Trim(), assessedAt);
    }
}