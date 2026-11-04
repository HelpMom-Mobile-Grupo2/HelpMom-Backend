using BackendMoviles.Domain.Common;

namespace BackendMoviles.Domain.Identity;

public sealed class User : Entity
{
    private User()
    {
        Email = null!;
        FullName = string.Empty;
        PasswordHash = string.Empty;
    }

    private User(
        Guid id,
        EmailAddress email,
        string fullName,
        string passwordHash,
        FamilyRole role,
        DateTimeOffset createdAt)
        : base(id)
    {
        Email = email;
        FullName = fullName;
        PasswordHash = passwordHash;
        Role = role;
        CreatedAt = createdAt;
        IsActive = true;
    }

    public EmailAddress Email { get; private set; }
    public string FullName { get; private set; }
    public string PasswordHash { get; private set; }
    public FamilyRole Role { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public bool IsActive { get; private set; }

    public static User Create(
        EmailAddress email,
        string fullName,
        string passwordHash,
        FamilyRole role = FamilyRole.Mother)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role));
        }

        return new User(Guid.NewGuid(), email, fullName.Trim(), passwordHash, role, DateTimeOffset.UtcNow);
    }

    public void Deactivate() => IsActive = false;
}
