using System.Net.Mail;

namespace BackendMoviles.Domain.Identity;

public sealed record EmailAddress
{
    private EmailAddress(string value) => Value = value;

    public string Value { get; }

    public static EmailAddress Create(string value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > 254)
        {
            throw new ArgumentException("A valid email address is required.", nameof(value));
        }

        try
        {
            var parsed = new MailAddress(normalized);
            if (!string.Equals(parsed.Address, normalized, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("A valid email address is required.", nameof(value));
            }
        }
        catch (FormatException exception)
        {
            throw new ArgumentException("A valid email address is required.", nameof(value), exception);
        }

        return new EmailAddress(normalized);
    }

    public override string ToString() => Value;
}