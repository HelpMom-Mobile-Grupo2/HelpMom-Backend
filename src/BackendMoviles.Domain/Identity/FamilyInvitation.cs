using System.Security.Cryptography;
using System.Text.RegularExpressions;
using BackendMoviles.Domain.Common;

namespace BackendMoviles.Domain.Identity;

public sealed partial class FamilyInvitation : Entity
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    private FamilyInvitation() => Code = string.Empty;

    private FamilyInvitation(Guid id, Guid familyGroupId, string code, DateTimeOffset createdAt)
        : base(id)
    {
        FamilyGroupId = familyGroupId;
        Code = code;
        CreatedAt = createdAt;
        ExpiresAt = createdAt.Add(Lifetime);
    }

    public Guid FamilyGroupId { get; private set; }
    public string Code { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public Guid? UsedByUserId { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    public static FamilyInvitation Create(Guid familyGroupId, string code, DateTimeOffset createdAt)
    {
        if (familyGroupId == Guid.Empty)
        {
            throw new ArgumentException("Family group id cannot be empty.", nameof(familyGroupId));
        }

        var normalized = NormalizeCode(code);
        if (!IsValidCode(normalized))
        {
            throw new ArgumentException("Invitation code must have the format HM-0000.", nameof(code));
        }

        return new FamilyInvitation(Guid.NewGuid(), familyGroupId, normalized, createdAt);
    }

    public static string GenerateCode() => $"HM-{RandomNumberGenerator.GetInt32(0, 10_000):D4}";

    public static string NormalizeCode(string? code) => code?.Trim().ToUpperInvariant() ?? string.Empty;

    public static bool IsValidCode(string? code) => code is not null && CodePattern().IsMatch(code);

    public bool IsExpired(DateTimeOffset now) => ExpiresAt <= now;

    public bool IsActive(DateTimeOffset now) => !IsUsed && !IsExpired(now);

    // An invalidated invitation simply expires now, so it keeps the used / not used state.
    public void Invalidate(DateTimeOffset now)
    {
        if (IsActive(now))
        {
            ExpiresAt = now;
        }
    }

    public void MarkUsed(Guid userId, DateTimeOffset now)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        if (!IsActive(now))
        {
            throw new InvalidOperationException("The invitation is no longer active.");
        }

        IsUsed = true;
        UsedByUserId = userId;
        UsedAt = now;
    }

    [GeneratedRegex(@"^HM-\d{4}$")]
    private static partial Regex CodePattern();
}
