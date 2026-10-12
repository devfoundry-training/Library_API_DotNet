using Library.Api.Data;
using Library.Api.Data.Repositories;
using Library.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Library.UnitTests;

/// <summary>
/// Each test gets a fresh in-memory SQLite database, built by the real migrations and loaded with
/// the sample data, and the services on top of it. No web server and no clock: today is passed in.
/// </summary>
public abstract class ServiceTestBase : IAsyncLifetime
{
    protected static readonly DateOnly Today = new(2026, 10, 12);

    // An in-memory database lives as long as its connection stays open.
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected SqliteLibraryDbContext Db { get; private set; } = null!;
    protected BookService Books { get; private set; } = null!;
    protected MemberService Members { get; private set; } = null!;
    protected LoanService Loans { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _connection.OpenAsync();
        Db = new SqliteLibraryDbContext(
            new DbContextOptionsBuilder<SqliteLibraryDbContext>().UseSqlite(_connection).Options);
        await Db.Database.MigrateAsync();
        await new LibrarySeeder(Db).SeedIfEmptyAsync();

        var bookRepository = new BookRepository(Db);
        var memberRepository = new MemberRepository(Db);
        Books = new BookService(bookRepository);
        Members = new MemberService(memberRepository);
        Loans = new LoanService(new LoanRepository(Db), bookRepository, memberRepository);
    }

    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync();
        await _connection.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected static DateOnly Date(string text) => DateOnly.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

    protected async Task<int> AvailableAsync(int bookId) => (await Books.GetAsync(bookId)).Available;
}
