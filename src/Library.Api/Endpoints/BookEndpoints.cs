using Library.Api.Contracts;
using Library.Api.Services;

namespace Library.Api.Endpoints;

public static class BookEndpoints
{
    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        var books = app.MapGroup("/books");
        books.MapGet("", List);
        books.MapPost("", Create);
        // {id:int}: /books/abc matches no route, so it is the normal 404 not_found.
        books.MapGet("/{id:int}", Get);
        books.MapPut("/{id:int}", Update);
        books.MapDelete("/{id:int}", Delete);
        return app;
    }

    private static async Task<IResult> List(BookService service, string? title, string? author, string? genre)
    {
        var books = await service.ListAsync(title, author, genre);
        return Results.Ok(books.Select(BookResponse.From));
    }

    private static async Task<IResult> Get(int id, BookService service) =>
        Results.Ok(BookResponse.From(await service.GetAsync(id)));

    private static async Task<IResult> Create(HttpRequest request, BookService service)
    {
        var book = BookResponse.From(await service.CreateAsync(await Requests.ReadBookAsync(request)));
        return Results.Created($"/books/{book.Id}", book);
    }

    private static async Task<IResult> Update(int id, HttpRequest request, BookService service) =>
        Results.Ok(BookResponse.From(await service.UpdateAsync(id, await Requests.ReadBookAsync(request))));

    private static async Task<IResult> Delete(int id, BookService service)
    {
        await service.DeleteAsync(id);
        return Results.NoContent();
    }
}
