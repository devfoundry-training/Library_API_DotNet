using Npgsql;

namespace Library.Api.Data;

public enum DatabaseProvider
{
    Sqlite,
    PostgreSql,
}

/// <summary>
/// Turns DATABASE_URL into a provider and an ADO.NET connection string. This is the one place
/// that knows both forms.
///   sqlite:///library.db                               -> Data Source=library.db
///   postgresql://library:library@db:5432/library      -> Host=db;Port=5432;Database=library;...
/// </summary>
public sealed record DatabaseUrl(DatabaseProvider Provider, string ConnectionString)
{
    public static DatabaseUrl Parse(string url)
    {
        if (url.StartsWith("sqlite:///", StringComparison.Ordinal))
        {
            // Three slashes then a relative path; four slashes for an absolute path, as in SQLAlchemy.
            var path = url["sqlite:///".Length..];
            if (path.Length == 0)
            {
                throw new InvalidOperationException("DATABASE_URL needs a file name after sqlite:///");
            }
            return new DatabaseUrl(DatabaseProvider.Sqlite, $"Data Source={path}");
        }

        if (url.StartsWith("postgresql://", StringComparison.Ordinal) || url.StartsWith("postgres://", StringComparison.Ordinal))
        {
            var uri = new Uri(url);
            var userInfo = uri.UserInfo.Split(':', 2);
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.IsDefaultPort || uri.Port < 0 ? 5432 : uri.Port,
                Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
                Username = Uri.UnescapeDataString(userInfo[0]),
                // No Kerberos here; without this Npgsql looks for libgssapi_krb5, which slim images lack.
                GssEncryptionMode = GssEncryptionMode.Disable,
            };
            if (userInfo.Length == 2)
            {
                builder.Password = Uri.UnescapeDataString(userInfo[1]);
            }
            return new DatabaseUrl(DatabaseProvider.PostgreSql, builder.ConnectionString);
        }

        throw new InvalidOperationException(
            "DATABASE_URL must start with sqlite:/// or postgresql:// (see the README)");
    }
}
