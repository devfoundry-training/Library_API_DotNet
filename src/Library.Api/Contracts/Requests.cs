using Library.Api.Errors;
using Library.Api.Services;

namespace Library.Api.Contracts;

/// <summary>
/// Request bodies: each one checks the JSON shape (fields present, right JSON types) and hands the
/// service plain values. The rules on those values (ranges, blanks, formats) live in the services.
/// </summary>
public static class Requests
{
    public static async Task<BookData> ReadBookAsync(HttpRequest request)
    {
        var body = await JsonBody.ReadObjectAsync(request);
        return new BookData(
            Title: body.RequiredText("title"),
            Author: body.RequiredText("author"),
            Year: body.RequiredWholeNumber("year"),
            Genre: body.RequiredText("genre"),
            Copies: body.RequiredWholeNumber("copies"),
            Format: body.RequiredText("format"),
            Length: body.RequiredWholeNumber("length"));
    }

    /// <summary>POST /members: joined may be left out.</summary>
    public static async Task<MemberData> ReadNewMemberAsync(HttpRequest request)
    {
        var body = await JsonBody.ReadObjectAsync(request);
        return new MemberData(body.RequiredText("name"), body.RequiredText("email"), body.RequiredText("type"),
            body.OptionalDate("joined"));
    }

    /// <summary>PUT /members/{id}: every field is required.</summary>
    public static async Task<MemberData> ReadMemberAsync(HttpRequest request)
    {
        var body = await JsonBody.ReadObjectAsync(request);
        return new MemberData(body.RequiredText("name"), body.RequiredText("email"), body.RequiredText("type"),
            body.RequiredDate("joined"));
    }

    public static async Task<(int MemberId, int BookId)> ReadBorrowAsync(HttpRequest request)
    {
        const string message = "body must be a JSON object with whole-number member_id and book_id";
        var body = await JsonBody.ReadObjectAsync(request, message);
        if (body.TryGetProperty("member_id", out var memberId) && JsonBody.TryWholeNumber(memberId, out var member)
            && body.TryGetProperty("book_id", out var bookId) && JsonBody.TryWholeNumber(bookId, out var book))
        {
            return (member, book);
        }
        throw ApiException.Validation(message);
    }
}
