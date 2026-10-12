using Library.Api.Contracts;
using Library.Api.Services;

namespace Library.Api.Errors;

/// <summary>
/// The one place that writes the error format {"error": {"code", "message"}}:
///   - exceptions from the services and the API layer get their status and code
///   - anything unexpected is 500 internal_error, with no details for the client
///   - the framework's empty 400, 404 and 405 answers (no route, wrong method, /books/abc)
///     get a JSON body
/// </summary>
public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var (status, code, message) = exception switch
            {
                ApiException e => (e.Status, e.Code, e.Message),
                ValidationError e => (StatusCodes.Status400BadRequest, e.Code, e.Message),
                NotFoundError e => (StatusCodes.Status404NotFound, e.Code, e.Message),
                RuleBroken e => (StatusCodes.Status409Conflict, e.Code, e.Message),
                BadHttpRequestException => (StatusCodes.Status400BadRequest, "validation_error", "the request could not be read"),
                _ => (StatusCodes.Status500InternalServerError, "internal_error", "unexpected problem"),
            };
            if (status == StatusCodes.Status500InternalServerError)
            {
                logger.LogError(exception, "Unexpected problem on {Method} {Path}", context.Request.Method, context.Request.Path);
            }

            context.Response.Clear();
            await WriteErrorAsync(context, status, code, message);
            return;
        }

        if (!context.Response.HasStarted && context.Response.ContentType is null)
        {
            switch (context.Response.StatusCode)
            {
                case StatusCodes.Status400BadRequest:
                    await WriteErrorAsync(context, 400, "validation_error", "the request is not valid");
                    break;
                case StatusCodes.Status404NotFound:
                    await WriteErrorAsync(context, 404, "not_found", "not found");
                    break;
                case StatusCodes.Status405MethodNotAllowed:
                    await WriteErrorAsync(context, 405, "method_not_allowed", "method not allowed");
                    break;
            }
        }
    }

    private static Task WriteErrorAsync(HttpContext context, int status, string code, string message)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(ErrorResponse.Of(code, message));
    }
}
