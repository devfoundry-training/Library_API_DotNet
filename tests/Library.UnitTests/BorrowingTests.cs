using Library.Api.Services;

namespace Library.UnitTests;

public class BorrowingTests : ServiceTestBase
{
    [Fact]
    public async Task Borrow_creates_loan_13_and_reduces_available()
    {
        var loan = await Loans.BorrowAsync(memberId: 101, bookId: 4, Today);

        Assert.Equal(13, loan.Loan.Id);
        Assert.Equal((Today, Date("2026-10-26"), false, 0, 0),
            (loan.Loan.Borrowed, loan.Due, loan.Overdue, loan.DaysLate, loan.Fine));
        Assert.Equal(2, await AvailableAsync(4));
    }

    [Fact]
    public async Task Return_restores_available()
    {
        Assert.Equal(0, await AvailableAsync(5));
        await Loans.ReturnAsync(4, Today);
        Assert.Equal(1, await AvailableAsync(5));
    }

    [Fact]
    public async Task Unknown_member_is_refused()
    {
        var error = await Assert.ThrowsAsync<MemberNotFound>(() => Loans.BorrowAsync(999, 1, Today));
        Assert.Equal("no member with id 999", error.Message);
    }

    [Fact]
    public async Task Unknown_book_is_refused()
    {
        var error = await Assert.ThrowsAsync<BookNotFound>(() => Loans.BorrowAsync(101, 99, Today));
        Assert.Equal("no book with id 99", error.Message);
    }

    [Fact]
    public async Task Member_is_checked_before_book()
    {
        await Assert.ThrowsAsync<MemberNotFound>(() => Loans.BorrowAsync(999, 99, Today));
    }

    [Fact]
    public async Task No_copies_available_is_refused()
    {
        var error = await Assert.ThrowsAsync<NoCopiesAvailable>(() => Loans.BorrowAsync(101, 3, Today));
        Assert.Equal("The Hobbit has no copies available", error.Message);
        Assert.Equal("no_copies_available", error.Code);
    }

    [Fact]
    public async Task Standard_limit_is_3_loans()
    {
        await Loans.BorrowAsync(105, 2, Today); // Ella Brown had 2 open loans
        var error = await Assert.ThrowsAsync<LoanLimitReached>(() => Loans.BorrowAsync(105, 7, Today));
        Assert.Equal("Ella Brown already has 3 loans (limit 3)", error.Message);
    }

    [Fact]
    public async Task Student_limit_is_5_loans()
    {
        foreach (var bookId in new[] { 1, 2, 4, 6 })
        {
            await Loans.BorrowAsync(102, bookId, Today); // Ben Taylor (student) had 1 open loan
        }
        var error = await Assert.ThrowsAsync<LoanLimitReached>(() => Loans.BorrowAsync(102, 7, Today));
        Assert.Equal("Ben Taylor already has 5 loans (limit 5)", error.Message);
    }

    [Fact]
    public async Task Limit_is_checked_before_copies()
    {
        await Loans.BorrowAsync(105, 2, Today);
        await Assert.ThrowsAsync<LoanLimitReached>(() => Loans.BorrowAsync(105, 3, Today));
    }

    [Fact]
    public async Task A_member_may_borrow_a_second_copy_of_the_same_book()
    {
        await Loans.BorrowAsync(101, 4, Today);
        await Loans.BorrowAsync(101, 4, Today);
        Assert.Equal(1, await AvailableAsync(4));
    }

    [Fact]
    public async Task A_refused_borrow_changes_nothing()
    {
        await Assert.ThrowsAsync<NoCopiesAvailable>(() => Loans.BorrowAsync(101, 3, Today));
        Assert.Equal(12, (await Loans.ListAsync(null, null, null, Today)).Count);
        Assert.Equal(13, (await Loans.BorrowAsync(101, 4, Today)).Loan.Id);
    }
}
