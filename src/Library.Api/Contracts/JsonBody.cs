using System.Globalization;
using System.Text.Json;
using Library.Api.Errors;

namespace Library.Api.Contracts;

/// <summary>
/// Reads request bodies strictly. Bodies are read as a JsonElement rather than bound to a class,
/// so "101", true and 4.5 are refused as whole numbers and every problem is a 400 validation_error
/// in the contract's format.
/// </summary>
public static class JsonBody
{
    public static async Task<JsonElement> ReadObjectAsync(HttpRequest request, string message = "body must be a JSON object")
    {
        try
        {
            using var document = await JsonDocument.ParseAsync(request.Body);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw ApiException.Validation(message);
            }
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            throw ApiException.Validation(message);
        }
    }

    public static string RequiredText(this JsonElement body, string name)
    {
        var value = Required(body, name);
        return value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : throw ApiException.Validation($"{name} must be text");
    }

    public static int RequiredWholeNumber(this JsonElement body, string name) =>
        TryWholeNumber(Required(body, name), out var number)
            ? number
            : throw ApiException.Validation($"{name} must be a whole number");

    public static DateOnly RequiredDate(this JsonElement body, string name) => ToDate(Required(body, name), name);

    /// <summary>A date that may be left out (or sent as null).</summary>
    public static DateOnly? OptionalDate(this JsonElement body, string name) =>
        body.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null
            ? ToDate(value, name)
            : null;

    /// <summary>A JSON number that is a whole number and fits in 32 bits. Text, true and 4.5 are not.</summary>
    public static bool TryWholeNumber(JsonElement value, out int number)
    {
        number = 0;
        return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out number);
    }

    private static JsonElement Required(JsonElement body, string name) =>
        body.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null
            ? value
            : throw ApiException.Validation($"{name} is required");

    private static DateOnly ToDate(JsonElement value, string name) =>
        value.ValueKind == JsonValueKind.String
        && DateOnly.TryParseExact(value.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : throw ApiException.Validation($"{name} must be a date in the form YYYY-MM-DD");
}
