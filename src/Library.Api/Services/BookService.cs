using Library.Api.Data.Entities;
using Library.Api.Data.Repositories;

namespace Library.Api.Services;

/// <summary>The fields of a book as a client sends them (create and replace).</summary>
public sealed record BookData(string Title, string Author, int Year, string Genre, int Copies, string Format, int Length);

/// <summary>A book with its available copies worked out.</summary>
public sealed record BookDetails(Book Book, int Available);

public sealed class BookService(BookRepository books)
{
    private static readonly string[] Formats = ["print", "ebook", "audio"];

    public async Task<List<BookDetails>> ListAsync(string? title, string? author, string? genre)
    {
        var found = await books.ListAsync(title, author, genre);
        return found.Select(b => Describe(b.Book, b.OnLoan)).ToList();
    }

    public async Task<BookDetails> GetAsync(int id)
    {
        var book = await books.FindAsync(id) ?? throw new BookNotFound(id);
        return Describe(book, await books.CountOnLoanAsync(id));
    }

    public async Task<BookDetails> CreateAsync(BookData data)
    {
        Validate(data);
        var book = new Book();
        Copy(data, book);
        books.Add(book);
        await books.SaveChangesAsync();
        return Describe(book, onLoan: 0);
    }

    /// <summary>Replaces every field. Copies cannot go below the copies on loan.</summary>
    public async Task<BookDetails> UpdateAsync(int id, BookData data)
    {
        Validate(data);
        var book = await books.FindAsync(id) ?? throw new BookNotFound(id);
        var onLoan = await books.CountOnLoanAsync(id);
        if (data.Copies < onLoan)
        {
            throw new CopiesBelowOnLoan(onLoan);
        }

        Copy(data, book);
        await books.SaveChangesAsync();
        return Describe(book, onLoan);
    }

    /// <summary>A book that has ever been lent stays, so the loan history is complete.</summary>
    public async Task DeleteAsync(int id)
    {
        var book = await books.FindAsync(id) ?? throw new BookNotFound(id);
        if (await books.HasAnyLoansAsync(id))
        {
            throw new BookHasLoans(book.Title);
        }

        books.Remove(book);
        await books.SaveChangesAsync();
    }

    // available = copies minus the open loans for that book
    private static BookDetails Describe(Book book, int onLoan) => new(book, book.Copies - onLoan);

    private static void Copy(BookData data, Book book)
    {
        book.Title = data.Title;
        book.Author = data.Author;
        book.Year = data.Year;
        book.Genre = data.Genre;
        book.Copies = data.Copies;
        book.Format = data.Format;
        book.Length = data.Length;
    }

    private static void Validate(BookData data)
    {
        Rules.Text("title", data.Title, maxLength: 100);
        Rules.Text("author", data.Author, maxLength: 100);
        Rules.Text("genre", data.Genre, maxLength: 50);
        if (data.Year is < 1450 or > 2100)
        {
            throw new ValidationError("year must be between 1450 and 2100");
        }
        if (data.Copies < 0)
        {
            throw new ValidationError("copies must not be negative");
        }
        if (!Formats.Contains(data.Format))
        {
            throw new ValidationError($"format must be print, ebook or audio, got '{data.Format}'");
        }
        if (data.Length <= 0)
        {
            throw new ValidationError("length must be positive");
        }
    }
}
