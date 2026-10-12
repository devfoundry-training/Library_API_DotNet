using Library.Api.Data.Entities;
using Library.Api.Services;

namespace Library.Api.Contracts;

// Response bodies. Property names become snake_case JSON (see Program.cs), dates are YYYY-MM-DD
// and money is whole pence.

public sealed record HealthResponse(string Status);

public sealed record BookResponse(
    int Id, string Title, string Author, int Year, string Genre, int Copies, string Format, int Length, int Available)
{
    public static BookResponse From(BookDetails details)
    {
        var b = details.Book;
        return new(b.Id, b.Title, b.Author, b.Year, b.Genre, b.Copies, b.Format, b.Length, details.Available);
    }
}

public sealed record MemberResponse(int Id, string Name, string Email, string Type, DateOnly Joined)
{
    public static MemberResponse From(Member m) => new(m.Id, m.Name, m.Email, m.Type, m.Joined);
}

public sealed record LoanResponse(
    int Id, int BookId, int MemberId, DateOnly Borrowed, DateOnly Due, DateOnly? Returned,
    bool Overdue, int DaysLate, int Fine)
{
    public static LoanResponse From(LoanDetails details)
    {
        var l = details.Loan;
        return new(l.Id, l.BookId, l.MemberId, l.Borrowed, details.Due, l.Returned,
            details.Overdue, details.DaysLate, details.Fine);
    }
}

public sealed record ErrorResponse(ErrorDetail Error)
{
    public static ErrorResponse Of(string code, string message) => new(new ErrorDetail(code, message));
}

public sealed record ErrorDetail(string Code, string Message);
