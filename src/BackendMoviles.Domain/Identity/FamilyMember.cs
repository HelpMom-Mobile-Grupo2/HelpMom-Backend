using BackendMoviles.Domain.Common;

namespace BackendMoviles.Domain.Identity;

public sealed class FamilyMember : Entity
{
    private FamilyMember()
    {
    }

    internal FamilyMember(Guid id, Guid userId, FamilyRole role, DateTimeOffset joinedAt)
        : base(id)
    {
        UserId = userId == Guid.Empty ? throw new ArgumentException("User id cannot be empty.", nameof(userId)) : userId;
        Role = role;
        JoinedAt = joinedAt;
    }

    public Guid UserId { get; private set; }
    public FamilyRole Role { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }
}