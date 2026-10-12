using Library.Api.Data;

namespace Library.Api.Endpoints;

/// <summary>
/// Mapped only when TEST_MODE=1 (see Program.cs). Without it POST /test/reset does not exist and is
/// the normal 404. Never turn test mode on for a deployed server: any caller could wipe the data.
/// </summary>
public static class TestEndpoints
{
    public static IEndpointRouteBuilder MapTestEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/test/reset", async (LibrarySeeder seeder) =>
        {
            await seeder.ResetAsync();
            return Results.NoContent();
        });
        return app;
    }
}
