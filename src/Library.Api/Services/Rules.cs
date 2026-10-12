namespace Library.Api.Services;

/// <summary>Validation rules shared by the services.</summary>
internal static class Rules
{
    /// <summary>Text that is not blank (empty or only spaces) and not too long. Text is never trimmed.</summary>
    public static void Text(string field, string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationError($"{field} must not be blank");
        }
        if (value.Length > maxLength)
        {
            throw new ValidationError($"{field} must be at most {maxLength} characters");
        }
    }
}
