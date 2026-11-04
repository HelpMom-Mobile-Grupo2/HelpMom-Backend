using BackendMoviles.Domain.Common;

namespace BackendMoviles.Domain.Companion;

public sealed class TipCompletion : Entity
{
    private TipCompletion()
    {
    }

    private TipCompletion(Guid id, Guid tipId, Guid partnerUserId, DateOnly date)
        : base(id)
    {
        TipId = tipId;
        PartnerUserId = partnerUserId;
        Date = date;
    }

    public Guid TipId { get; private set; }
    public Guid PartnerUserId { get; private set; }
    public DateOnly Date { get; private set; }

    public static TipCompletion Create(Guid tipId, Guid partnerUserId, DateOnly date)
    {
        if (tipId == Guid.Empty)
        {
            throw new ArgumentException("Tip id cannot be empty.", nameof(tipId));
        }

        if (partnerUserId == Guid.Empty)
        {
            throw new ArgumentException("Partner user id cannot be empty.", nameof(partnerUserId));
        }

        return new TipCompletion(Guid.NewGuid(), tipId, partnerUserId, date);
    }
}
