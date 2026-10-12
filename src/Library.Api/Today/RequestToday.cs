using System.Globalization;
using Library.Api.Errors;

namespace Library.Api.Today;

/// <summary>
/// The one place that answers "what is today", in the contract's order:
///   1. the X-Today header, only when TEST_MODE=1
///   2. LIBRARY_TODAY
///   3. the current date in Europe/London, from TimeProvider
/// Scoped, so it is worked out once per request.
/// </summary>
public sealed class RequestToday(IHttpContextAccessor http, TimeProvider clock, LibrarySettings settings) : IToday
{
    private static readonly TimeZoneInfo London = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

    private DateOnly? _value;

    public DateOnly Value => _value ??= Resolve();

    private DateOnly Resolve()
    {
        if (settings.TestMode && http.HttpContext?.Request.Headers.TryGetValue("X-Today", out var header) == true)
        {
            if (!DateOnly.TryParseExact(header.ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var fromHeader))
            {
                throw ApiException.Validation("X-Today must be a date in the form YYYY-MM-DD");
            }
            return fromHeader;
        }

        if (settings.FixedToday is { } fixedToday)
        {
            return fixedToday;
        }

        var londonNow = TimeZoneInfo.ConvertTime(clock.GetUtcNow(), London);
        return DateOnly.FromDateTime(londonNow.DateTime);
    }
}
