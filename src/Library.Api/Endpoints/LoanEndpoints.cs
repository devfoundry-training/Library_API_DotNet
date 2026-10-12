using Library.Api.Contracts;
using Library.Api.Services;
using Library.Api.Today;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Endpoints;

/// <summary>Loans cannot be changed or deleted, only returned, so there is no PUT or DELETE (405).</summary>
public static class LoanEndpoints
{
    public static IEndpointRouteBuilder MapLoanEndpoints(this IEndpointRouteBuilder app)
    {
        var loans = app.MapGroup("/loans");
        loans.MapGet("", List);
        loans.MapPost("", Borrow);
        loans.MapGet("/{id:int}", Get);
        loans.MapPost("/{id:int}/return", Return);
        return app;
    }

    // Query values arrive as text and are checked here, so member_id=abc is a 400 in our format.
    private static async Task<IResult> List(
        LoanService service, IToday today,
        [FromQuery(Name = "member_id")] string? memberId, [FromQuery] string? active, [FromQuery] string? overdue)
    {
        var loans = await service.ListAsync(
            QueryValues.WholeNumber(memberId, "member_id"),
            QueryValues.TrueOrFalse(active, "active"),
            QueryValues.TrueOrFalse(overdue, "overdue"),
            today.Value);
        return Results.Ok(loans.Select(LoanResponse.From));
    }

    private static async Task<IResult> Get(int id, LoanService service, IToday today) =>
        Results.Ok(LoanResponse.From(await service.GetAsync(id, today.Value)));

    private static async Task<IResult> Borrow(HttpRequest request, LoanService service, IToday today)
    {
        var (memberId, bookId) = await Requests.ReadBorrowAsync(request);
        var loan = LoanResponse.From(await service.BorrowAsync(memberId, bookId, today.Value));
        return Results.Created($"/loans/{loan.Id}", loan);
    }

    private static async Task<IResult> Return(int id, LoanService service, IToday today) =>
        Results.Ok(LoanResponse.From(await service.ReturnAsync(id, today.Value)));
}
