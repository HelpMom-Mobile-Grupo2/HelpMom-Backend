using BackendMoviles.Domain.Common;

namespace BackendMoviles.Domain.Identity;

public sealed class FamilyGroup : Entity
{
    private readonly List<FamilyMember> _members = [];

    private FamilyGroup() => Name = string.Empty;

    private FamilyGroup(Guid id, string name, Guid motherUserId) : base(id)
    {
        Name = name;
        _members.Add(new FamilyMember(Guid.NewGuid(), motherUserId, FamilyRole.Mother, DateTimeOffset.UtcNow));
    }

    public string Name { get; private set; }
    public IReadOnlyCollection<FamilyMember> Members => _members.AsReadOnly();

    public static FamilyGroup Create(string name, Guid motherUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (motherUserId == Guid.Empty)
        {
            throw new ArgumentException("Mother user id cannot be empty.", nameof(motherUserId));
        }

        return new FamilyGroup(Guid.NewGuid(), name.Trim(), motherUserId);
    }

    public FamilyMember AddMember(Guid userId, FamilyRole role)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        if (!Enum.IsDefined(role) || role == FamilyRole.Mother)
        {
            throw new ArgumentOutOfRangeException(nameof(role), "A family group has one mother account.");
        }

        if (_members.Any(member => member.UserId == userId))
        {
            throw new InvalidOperationException("The user already belongs to this family group.");
        }

        var member = new FamilyMember(Guid.NewGuid(), userId, role, DateTimeOffset.UtcNow);
        _members.Add(member);
        return member;
    }

    public void RemoveMember(Guid userId)
    {
        var member = _members.SingleOrDefault(item => item.UserId == userId);
        if (member is null)
        {
            throw new InvalidOperationException("The user is not a member of this family group.");
        }

        if (member.Role == FamilyRole.Mother)
        {
            throw new InvalidOperationException("The mother account cannot be removed from its family group.");
        }

        _members.Remove(member);
    }
}