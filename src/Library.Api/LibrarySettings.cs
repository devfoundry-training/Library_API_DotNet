using System.Globalization;

namespace Library.Api;

/// <summary>
/// The four environment variables from the contract, read once at start-up.
/// </summary>
public sealed record LibrarySettings(int Port, string DatabaseUrl, bool TestMode, DateOnly? FixedToday)
{
    public const string DefaultDatabaseUrl = "sqlite:///library.db";

    public static LibrarySettings FromEnvironment() => From(Environment.GetEnvironmentVariable);

    public static LibrarySettings From(Func<string, string?> read)
    {
        var port = 8000;
        var portText = read("PORT");
        if (!string.IsNullOrEmpty(portText) && !int.TryParse(portText, NumberStyles.None, CultureInfo.InvariantCulture, out port))
        {
            throw new InvalidOperationException($"PORT must be a whole number, got '{portText}'");
        }

        var databaseUrl = read("DATABASE_URL");
        if (string.IsNullOrEmpty(databaseUrl))
        {
            databaseUrl = DefaultDatabaseUrl;
        }

        DateOnly? fixedToday = null;
        var todayText = read("LIBRARY_TODAY");
        if (!string.IsNullOrEmpty(todayText))
        {
            if (!DateOnly.TryParseExact(todayText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                throw new InvalidOperationException($"LIBRARY_TODAY must be a date in the form YYYY-MM-DD, got '{todayText}'");
            }
            fixedToday = date;
        }

        // Only exactly "1" turns test mode on.
        return new LibrarySettings(port, databaseUrl, read("TEST_MODE") == "1", fixedToday);
    }
}
