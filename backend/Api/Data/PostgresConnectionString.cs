using Npgsql;

namespace Api.Data;

public static class PostgresConnectionString
{
    /// <summary>
    /// Accepts either an Npgsql keyword string ("Host=…;Database=…") or a URL ("postgresql://user:pass@host:5432/db"),
    /// which is how Render (and most hosts) hand out database addresses; Npgsql itself only understands the former.
    /// </summary>
    public static string Normalize(string connectionString)
    {
        if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return connectionString;

        var uri = new Uri(connectionString);
        var credentials = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.DnsSafeHost, // IPv6 literals without the URL's [brackets]
            Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : null,
        };

        // Only ?sslmode=… is understood (external URLs need TLS; Render's private network doesn't). Anything else, or an
        // unknown sslmode, fails loudly: silently ignoring "verify-full" would quietly downgrade to an unverified connection.
        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var libpqValue = parts.Length == 2 ? parts[1].Replace("-", "").Replace("_", "") : ""; // verify-full -> VerifyFull
            if (parts.Length != 2 || !parts[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase)
                || !Enum.TryParse<SslMode>(libpqValue, ignoreCase: true, out var sslMode))
                throw new InvalidOperationException($"Unsupported option in the database URL: '{parts[0]}'.");

            builder.SslMode = sslMode;
        }

        return builder.ConnectionString;
    }
}
