using BackendMoviles.Domain.Common;

namespace BackendMoviles.Domain.Triage;

public sealed class TriageMessage : Entity
{
    private TriageMessage() => Content = string.Empty;

    internal TriageMessage(Guid id, MessageAuthor author, string content, DateTimeOffset sentAt)
        : base(id)
    {
        Author = author;
        Content = content;
        SentAt = sentAt;
    }

    public MessageAuthor Author { get; private set; }
    public string Content { get; private set; }
    public DateTimeOffset SentAt { get; private set; }

    internal static TriageMessage Create(MessageAuthor author, string content, DateTimeOffset sentAt)
    {
        if (!Enum.IsDefined(author))
        {
            throw new ArgumentOutOfRangeException(nameof(author));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        if (content.Length > 4000)
        {
            throw new ArgumentException("Message cannot exceed 4000 characters.", nameof(content));
        }

        return new TriageMessage(Guid.NewGuid(), author, content.Trim(), sentAt);
    }
}