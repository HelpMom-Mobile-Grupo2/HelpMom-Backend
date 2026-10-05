using BackendMoviles.Domain.Common;

namespace BackendMoviles.Domain.Triage;

public sealed class TriageConversation : Entity
{
    private readonly List<TriageMessage> _messages = [];

    private TriageConversation()
    {
    }

    private TriageConversation(Guid id, Guid motherUserId) : base(id)
    {
        MotherUserId = motherUserId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid MotherUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<TriageMessage> Messages => _messages.AsReadOnly();
    public TriageAssessment? LatestAssessment { get; private set; }

    public static TriageConversation Start(Guid motherUserId)
    {
        if (motherUserId == Guid.Empty)
        {
            throw new ArgumentException("Mother user id cannot be empty.", nameof(motherUserId));
        }

        return new TriageConversation(Guid.NewGuid(), motherUserId);
    }

    public TriageMessage AddMessage(MessageAuthor author, string content, DateTimeOffset sentAt)
    {
        var message = TriageMessage.Create(author, content, sentAt);
        _messages.Add(message);
        return message;
    }

    public void SetAssessment(TriageAssessment assessment)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        LatestAssessment = assessment;
    }
}