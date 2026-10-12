using Library.Api.Services;

namespace Library.UnitTests;

public class BookAndMemberChangeTests : ServiceTestBase
{
    private static readonly BookData Dune = new("Dune", "Frank Herbert", 1965, "Science Fiction", 4, "print", 412);
    private static readonly BookData Piranesi = new("Piranesi", "Susanna Clarke", 2020, "Fantasy", 2, "print", 272);

    // -- books -----------------------------------------------------------

    [Fact]
    public async Task New_book_is_21_with_every_copy_available()
    {
        var book = await Books.CreateAsync(Piranesi);
        Assert.Equal((21, 2), (book.Book.Id, book.Available));
    }

    [Fact]
    public async Task Copies_cannot_go_below_the_copies_on_loan()
    {
        var error = await Assert.ThrowsAsync<CopiesBelowOnLoan>(() => Books.UpdateAsync(4, Dune with { Copies = 0 }));
        Assert.Equal("copies cannot be less than the 1 on loan", error.Message);

        var updated = await Books.UpdateAsync(4, Dune with { Copies = 1 });
        Assert.Equal(0, updated.Available);
    }

    [Theory]
    [InlineData(1, "Pride and Prejudice has loans and cannot be deleted")]
    [InlineData(13, "Brave New World has loans and cannot be deleted")] // only a returned loan
    public async Task A_book_with_loans_cannot_be_deleted(int id, string message)
    {
        var error = await Assert.ThrowsAsync<BookHasLoans>(() => Books.DeleteAsync(id));
        Assert.Equal(message, error.Message);
    }

    [Fact]
    public async Task A_book_without_loans_is_deleted()
    {
        await Books.DeleteAsync(8);
        await Assert.ThrowsAsync<BookNotFound>(() => Books.GetAsync(8));
    }

    public static TheoryData<BookData> InvalidBooks => new()
    {
        Piranesi with { Title = "   " },
        Piranesi with { Title = new string('T', 101) },
        Piranesi with { Author = "" },
        Piranesi with { Genre = new string('G', 51) },
        Piranesi with { Year = 1449 },
        Piranesi with { Year = 2101 },
        Piranesi with { Copies = -1 },
        Piranesi with { Format = "tablet" },
        Piranesi with { Length = 0 },
    };

    [Theory]
    [MemberData(nameof(InvalidBooks))]
    public async Task Invalid_books_are_refused(BookData data)
    {
        await Assert.ThrowsAsync<ValidationError>(() => Books.CreateAsync(data));
        Assert.Equal(20, (await Books.ListAsync(null, null, null)).Count);
    }

    [Fact]
    public async Task Boundary_values_are_accepted()
    {
        await Books.CreateAsync(Piranesi with { Year = 1450, Copies = 0, Length = 1 });
        await Books.CreateAsync(Piranesi with { Year = 2100, Title = new string('T', 100), Genre = new string('G', 50) });
    }

    [Fact]
    public async Task Search_ignores_case_and_genre_matches_whole()
    {
        var ids = async (string? title, string? author, string? genre) =>
            (await Books.ListAsync(title, author, genre)).Select(b => b.Book.Id).ToArray();

        Assert.Equal(new[] { 3, 6, 9, 10, 11, 14, 17, 20 }, await ids("the", null, null));
        Assert.Equal(new[] { 2, 7 }, await ids(null, "orwell", null));
        Assert.Equal(new[] { 4, 6, 9, 14, 20 }, await ids(null, null, "science fiction"));
        Assert.Empty(await ids(null, null, "science"));
        Assert.Equal(new[] { 6, 20 }, await ids(null, "wells", "Science Fiction"));
    }

    // -- members ---------------------------------------------------------

    private static readonly MemberData Iris = new("Iris Murdoch", "iris.murdoch@example.com", "student", null);

    [Fact]
    public async Task New_member_is_110_and_joins_today_by_default()
    {
        var member = await Members.CreateAsync(Iris, Today);
        Assert.Equal((110, Today), (member.Id, member.Joined));
    }

    [Fact]
    public async Task A_duplicate_email_is_refused_on_create()
    {
        var error = await Assert.ThrowsAsync<EmailTaken>(() =>
            Members.CreateAsync(Iris with { Email = "amira.khan@example.com" }, Today));
        Assert.Equal("email amira.khan@example.com is already used by member 101", error.Message);
    }

    [Fact]
    public async Task Another_members_email_is_refused_on_update_but_your_own_is_fine()
    {
        var ben = new MemberData("Ben Taylor", "ella.brown@example.com", "student", Date("2024-09-02"));
        var error = await Assert.ThrowsAsync<EmailTaken>(() => Members.UpdateAsync(102, ben));
        Assert.Equal("email ella.brown@example.com is already used by member 105", error.Message);

        var kept = await Members.UpdateAsync(102, ben with { Name = "Ben Taylor-Smith", Email = "ben.taylor@example.com" });
        Assert.Equal("Ben Taylor-Smith", kept.Name);
    }

    [Theory]
    [InlineData("   ", "iris@example.com", "student")]
    [InlineData("Iris", "not-an-email", "student")]
    [InlineData("Iris", "iris murdoch@example.com", "student")]
    [InlineData("Iris", "iris@example.com", "visitor")]
    public async Task Invalid_members_are_refused(string name, string email, string type)
    {
        await Assert.ThrowsAsync<ValidationError>(() => Members.CreateAsync(new MemberData(name, email, type, null), Today));
    }

    [Fact]
    public async Task A_type_change_moves_the_due_dates()
    {
        Assert.Equal(Date("2026-10-15"), (await Loans.GetAsync(5, Today)).Due);
        await Members.UpdateAsync(101,
            new MemberData("Amira Khan", "amira.khan@example.com", "staff", Date("2023-01-15")));
        Assert.Equal(Date("2026-10-29"), (await Loans.GetAsync(5, Today)).Due);
    }
}
