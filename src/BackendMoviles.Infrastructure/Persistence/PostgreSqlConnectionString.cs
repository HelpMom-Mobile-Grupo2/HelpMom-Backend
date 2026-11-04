using Microsoft.AspNetCore.WebUtilities;
using Npgsql;

namespace BackendMoviles.Infrastructure.Persistence;

public static class PostgreSqlConnectionString
{
    public const int DefaultPort = 5432;

    private const string FormatMessage =
        "DATABASE_URL must have the form postgres://user:password@host:port/database.";

    // Converts the URL that Render (and other hosts) expose in DATABASE_URL into an Npgsql connection string.
    // Accepts the postgres:// and postgresql:// schemes, an optional port and an optional ?sslmode=.
    public static string FromDatabaseUrl(string databaseUrl)
    {
        if (!Uri.TryCreate(databaseUrl.Trim(), UriKind.Absolute, out var uri)
            || uri.Scheme is not ("postgres" or "postgresql"))
        {
            throw new InvalidOperationException(FormatMessage);
        }

        var credentials = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(credentials[0]);
        var database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/'));
        if (string.IsNullOrEmpty(uri.Host) || username.Length == 0 || database.Length == 0)
        {
            throw new InvalidOperationException(FormatMessage);
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : DefaultPort,
            Database = database,
            Username = username,
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : null
        };

        var query = QueryHelpers.ParseQuery(uri.Query);
        if (query.TryGetValue("sslmode", out var sslMode))
        {
            // libpq spells the modes in kebab case (verify-full); Npgsql uses the same names without dashes.
            builder.SslMode = Enum.TryParse<SslMode>(sslMode.ToString().Replace("-", string.Empty), ignoreCase: true, out var mode)
                ? mode
                : throw new InvalidOperationException($"DATABASE_URL has an unsupported sslmode: {sslMode}.");
        }

        return builder.ConnectionString;
    }
}
