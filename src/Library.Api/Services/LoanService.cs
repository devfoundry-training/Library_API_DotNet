using Library.Api.Data.Entities;
using Library.Api.Data.Repositories;

namespace Library.Api.Services;

/// <summary>A loan with its due date, overdue flag, days late and fine worked out for a given day.</summary>
public sealed record LoanDetails(Loan Loan, DateOnly Due, bool Overdue, int DaysLate, int Fine);

public sealed class LoanService(LoanRepository loans, BookRepository books, MemberRepository members)
{
    /// <summary>
    /// Loans in id order. overdue is worked out from today, so it is filtered here rather than
    /// in the database.
    /// </summary>
    public async Task<List<LoanDetails>> ListAsync(int? memberId, bool? active, bool? overdue, DateOnly today)
    {
        var found = await loans.ListAsync(memberId, active);
        return found
            .Select(loan => Describe(loan, today))
            .Where(loan => overdue is null || loan.Overdue == overdue)
            .ToList();
    }

    /// <summary>One member's loans. Unlike the memberId filter above, an unknown member is a 404.</summary>
    public async Task<List<LoanDetails>> ListForMemberAsync(int memberId, bool? active, bool? overdue, DateOnly today)
    {
        if (!await members.ExistsAsync(memberId))
        {
            throw new MemberNotFound(memberId);
        }
        return await ListAsync(memberId, active, overdue, today);
    }

    public async Task<LoanDetails> GetAsync(int id, DateOnly today)
    {
        var loan = await loans.FindAsync(id) ?? throw new LoanNotFound(id);
        return Describe(loan, today);
    }

    /// <summary>
    /// The checks run in this order and the first failure decides the answer (task 07):
    /// member exists, book exists, member under the limit, a copy available.
    /// </summary>
    public async Task<LoanDetails> BorrowAsync(int memberId, int bookId, DateOnly today)
    {
        var member = await members.FindAsync(memberId) ?? throw new MemberNotFound(memberId);
        var book = await books.FindAsync(bookId) ?? throw new BookNotFound(bookId);

        var policy = LoanPolicy.For(member.Type);
        var open = await loans.CountOpenForMemberAsync(memberId);
        if (open >= policy.MaxLoans)
        {
            throw new LoanLimitReached(member.Name, open, policy.MaxLoans);
        }
        if (book.Copies - await books.CountOnLoanAsync(bookId) <= 0)
        {
            throw new NoCopiesAvailable(book.Title);
        }

        var loan = new Loan { BookId = bookId, Member = member, Borrowed = today };
        loans.Add(loan);
        await loans.SaveChangesAsync();
        return Describe(loan, today);
    }

    public async Task<LoanDetails> ReturnAsync(int id, DateOnly today)
    {
        var loan = await loans.FindAsync(id) ?? throw new LoanNotFound(id);
        if (loan.Returned is not null)
        {
            throw new AlreadyReturned(id);
        }
        if (today < loan.Borrowed)
        {
            throw new ReturnBeforeBorrowed(id, loan.Borrowed, today);
        }

        loan.Returned = today;
        await loans.SaveChangesAsync();
        return Describe(loan, today);
    }

    /// <summary>
    /// Due date, overdue, days late and fine for one loan on one day, from the member's current
    /// type. Nothing here is stored.
    /// </summary>
    public static LoanDetails Describe(Loan loan, DateOnly today)
    {
        var policy = LoanPolicy.For(loan.Member.Type);
        var due = policy.DueDate(loan.Borrowed);
        var end = loan.Returned ?? today;
        var daysLate = Math.Max(0, end.DayNumber - due.DayNumber);
        var overdue = loan.Returned is null && today > due;
        return new LoanDetails(loan, due, overdue, daysLate, daysLate * policy.FinePerDay);
    }
}
