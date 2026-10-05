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

    private User(Guid id, EmailAddress email, string fullName, string passwordHash, DateTimeOffset createdAt)
        : base(id)
    {
        Email = email;
        FullName = fullName;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
        IsActive = true;
    }

    public EmailAddress Email { get; private set; }
    public string FullName { get; private set; }
    public string PasswordHash { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public bool IsActive { get; private set; }

    public static User Create(EmailAddress email, string fullName, string passwordHash)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new User(Guid.NewGuid(), email, fullName.Trim(), passwordHash, DateTimeOffset.UtcNow);
    }

    public void Deactivate() => IsActive = false;
}