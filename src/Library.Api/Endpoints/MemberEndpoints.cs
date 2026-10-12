using Library.Api.Contracts;
using Library.Api.Services;
using Library.Api.Today;
using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Endpoints;

/// <summary>Members cannot be deleted in the basic API, so there is no MapDelete (405).</summary>
public static class MemberEndpoints
{
    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var members = app.MapGroup("/members");
        members.MapGet("", List);
        members.MapPost("", Create);
        members.MapGet("/{id:int}", Get);
        members.MapPut("/{id:int}", Update);
        members.MapGet("/{id:int}/loans", ListLoans);
        return app;
    }

    private static async Task<IResult> List(MemberService service) =>
        Results.Ok((await service.ListAsync()).Select(MemberResponse.From));

    private static async Task<IResult> Get(int id, MemberService service) =>
        Results.Ok(MemberResponse.From(await service.GetAsync(id)));

    private static async Task<IResult> Create(HttpRequest request, MemberService service, IToday today)
    {
        var data = await Requests.ReadNewMemberAsync(request);
        var member = MemberResponse.From(await service.CreateAsync(data, today.Value));
        return Results.Created($"/members/{member.Id}", member);
    }

    private static async Task<IResult> Update(int id, HttpRequest request, MemberService service) =>
        Results.Ok(MemberResponse.From(await service.UpdateAsync(id, await Requests.ReadMemberAsync(request))));

    private static async Task<IResult> ListLoans(
        int id, LoanService service, IToday today, [FromQuery] string? active, [FromQuery] string? overdue)
    {
        var loans = await service.ListForMemberAsync(id,
            QueryValues.TrueOrFalse(active, "active"), QueryValues.TrueOrFalse(overdue, "overdue"), today.Value);
        return Results.Ok(loans.Select(LoanResponse.From));
    }
}
