using System.Globalization;
using Library.Api.Errors;

namespace Library.Api.Contracts;

/// <summary>Query string values, checked strictly. Each one arrives as text and may be left out.</summary>
public static class QueryValues
{
    /// <summary>Exactly "true" or "false". TRUE, yes and 1 are refused.</summary>
    public static bool? TrueOrFalse(string? value, string name) => value switch
    {
        null => null,
        "true" => true,
        "false" => false,
        _ => throw ApiException.Validation($"{name} must be true or false"),
    };

    public static int? WholeNumber(string? value, string name)
    {
        if (value is null)
        {
            return null;
        }
        return int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number)
            ? number
            : throw ApiException.Validation($"{name} must be a whole number");
    }
}
