using Library.Api.Contracts;
using Library.Api.Data;

namespace Library.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        // "ok" means the server is running and can reach its database.
        app.MapGet("/health", async (LibraryDbContext db) =>
            await db.Database.CanConnectAsync()
                ? Results.Ok(new HealthResponse("ok"))
                : throw new InvalidOperationException("the database cannot be reached"));
        return app;
    }
}
