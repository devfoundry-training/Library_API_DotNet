namespace Library.Api.Errors;

/// <summary>
/// An error found by the API layer itself (a body that is not JSON, a bad query value, a bad
/// X-Today), before any service is called. Errors from the rules come from Services/LibraryErrors.cs.
/// </summary>
public sealed class ApiException(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;

    public static ApiException Validation(string message) =>
        new(StatusCodes.Status400BadRequest, "validation_error", message);
}
