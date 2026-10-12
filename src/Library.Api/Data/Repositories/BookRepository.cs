using Library.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Data.Repositories;

/// <summary>A book and the number of its copies on loan (loans with no returned date).</summary>
public sealed record BookOnLoan(Book Book, int OnLoan);

public sealed class BookRepository(LibraryDbContext db)
{
    /// <summary>
    /// Books in id order. Title and author match anywhere in the field, genre matches the whole
    /// genre, all ignoring case. A null filter is not applied.
    /// </summary>
    public Task<List<BookOnLoan>> ListAsync(string? title, string? author, string? genre)
    {
        var books = db.Books.AsNoTracking();
        if (title is not null)
        {
            books = books.Where(b => b.Title.ToLower().Contains(title.ToLower()));
        }
        if (author is not null)
        {
            books = books.Where(b => b.Author.ToLower().Contains(author.ToLower()));
        }
        if (genre is not null)
        {
            books = books.Where(b => b.Genre.ToLower() == genre.ToLower());
        }

        return books
            .OrderBy(b => b.Id)
            .Select(b => new BookOnLoan(b, b.Loans.Count(l => l.Returned == null)))
            .ToListAsync();
    }

    public Task<Book?> FindAsync(int id) => db.Books.SingleOrDefaultAsync(b => b.Id == id);

    public Task<int> CountOnLoanAsync(int bookId) =>
        db.Loans.CountAsync(l => l.BookId == bookId && l.Returned == null);

    public Task<bool> HasAnyLoansAsync(int bookId) => db.Loans.AnyAsync(l => l.BookId == bookId);

    public void Add(Book book) => db.Books.Add(book);

    public void Remove(Book book) => db.Books.Remove(book);

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
