using BackendMoviles.Domain.Common;

namespace BackendMoviles.Domain.Companion;

public sealed class QuickAlert : Entity
{
    public const int MaxMessageLength = 500;

    private QuickAlert() => Message = string.Empty;

    private QuickAlert(
        Guid id,
        Guid familyGroupId,
        Guid createdByUserId,
        QuickAlertType type,
        string message,
        DateTimeOffset createdAt)
        : base(id)
    {
        FamilyGroupId = familyGroupId;
        CreatedByUserId = createdByUserId;
        Type = type;
        Message = message;
        CreatedAt = createdAt;
        Status = QuickAlertStatus.Pending;
    }

    public Guid FamilyGroupId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public QuickAlertType Type { get; private set; }
    public string Message { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public QuickAlertStatus Status { get; private set; }
    public QuickAlertResponse? Response { get; private set; }
    public Guid? RespondedByUserId { get; private set; }
    public DateTimeOffset? RespondedAt { get; private set; }

    public static QuickAlert Create(
        Guid familyGroupId,
        Guid createdByUserId,
        QuickAlertType type,
        string? message,
        DateTimeOffset createdAt)
    {
        if (familyGroupId == Guid.Empty)
        {
            throw new ArgumentException("Family group id cannot be empty.", nameof(familyGroupId));
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new ArgumentException("Creator user id cannot be empty.", nameof(createdByUserId));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        var text = string.IsNullOrWhiteSpace(message) ? DefaultMessage(type) : message.Trim();
        if (text.Length > MaxMessageLength)
        {
            throw new ArgumentException($"Message cannot exceed {MaxMessageLength} characters.", nameof(message));
        }

        return new QuickAlert(Guid.NewGuid(), familyGroupId, createdByUserId, type, text, createdAt);
    }

    public static string DefaultMessage(QuickAlertType type) => type switch
    {
        QuickAlertType.Craving => "Tengo un antojo, ¿me ayudas a conseguir algo rico?",
        QuickAlertType.TiredOrPain => "Me siento cansada o con molestias, ¿puedes ayudarme?",
        QuickAlertType.ComeHere => "¿Puedes venir un momento? Te necesito.",
        QuickAlertType.Sos => "Necesito ayuda urgente. Comunícate conmigo ahora.",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public void Respond(Guid partnerUserId, QuickAlertResponse response, DateTimeOffset respondedAt)
    {
        if (partnerUserId == Guid.Empty)
        {
            throw new ArgumentException("Partner user id cannot be empty.", nameof(partnerUserId));
        }

        if (!Enum.IsDefined(response))
        {
            throw new ArgumentOutOfRangeException(nameof(response));
        }

        if (Status == QuickAlertStatus.Responded)
        {
            throw new InvalidOperationException("The alert has already been answered.");
        }

        Status = QuickAlertStatus.Responded;
        Response = response;
        RespondedByUserId = partnerUserId;
        RespondedAt = respondedAt;
    }
}
