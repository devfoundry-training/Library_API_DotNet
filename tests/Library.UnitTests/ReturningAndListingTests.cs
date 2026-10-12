using Library.Api.Services;

namespace Library.UnitTests;

public class ReturningAndListingTests : ServiceTestBase
{
    [Fact]
    public async Task Return_sets_returned_and_the_final_fine()
    {
        var loan = await Loans.ReturnAsync(4, Today); // Ella Brown, due 2026-10-04
        Assert.Equal((Today, false, 8, 200), (loan.Loan.Returned, loan.Overdue, loan.DaysLate, loan.Fine));
    }

    [Fact]
    public async Task Already_returned_is_refused()
    {
        var error = await Assert.ThrowsAsync<AlreadyReturned>(() => Loans.ReturnAsync(1, Today));
        Assert.Equal("loan 1 has already been returned", error.Message);
    }

    [Fact]
    public async Task Return_before_borrowed_is_refused_and_changes_nothing()
    {
        var error = await Assert.ThrowsAsync<ReturnBeforeBorrowed>(() => Loans.ReturnAsync(7, Date("2026-10-01")));
        Assert.Equal("loan 7 was borrowed on 2026-10-10 and cannot be returned on 2026-10-01", error.Message);

        Db.ChangeTracker.Clear();
        Assert.Null((await Loans.GetAsync(7, Today)).Loan.Returned);
    }

    [Fact]
    public async Task Returning_frees_a_place_under_the_limit()
    {
        await Loans.BorrowAsync(105, 2, Today);
        await Assert.ThrowsAsync<LoanLimitReached>(() => Loans.BorrowAsync(105, 7, Today));
        await Loans.ReturnAsync(4, Today);
        await Loans.BorrowAsync(105, 7, Today);
    }

    [Fact]
    public async Task Unknown_loan_is_refused()
    {
        var error = await Assert.ThrowsAsync<LoanNotFound>(() => Loans.GetAsync(99, Today));
        Assert.Equal("no loan with id 99", error.Message);
    }

    // The sample loans on 2026-10-12 (RULES.md, last table).
    [Theory]
    [InlineData(1, "2026-09-15", false, 0, 0)]
    [InlineData(2, "2026-10-01", false, 4, 40)]
    [InlineData(3, "2026-10-03", true, 9, 0)]
    [InlineData(4, "2026-10-04", true, 8, 200)]
    [InlineData(5, "2026-10-15", false, 0, 0)]
    [InlineData(6, "2026-09-18", true, 24, 240)]
    [InlineData(7, "2026-10-24", false, 0, 0)]
    [InlineData(8, "2026-10-06", true, 6, 60)]
    [InlineData(9, "2026-08-29", false, 12, 0)]
    [InlineData(10, "2026-10-12", false, 0, 0)]
    [InlineData(11, "2026-10-11", true, 1, 25)]
    [InlineData(12, "2026-09-28", false, 2, 50)]
    public async Task Sample_loans_are_worked_out(int id, string due, bool overdue, int daysLate, int fine)
    {
        var loan = await Loans.GetAsync(id, Today);
        Assert.Equal((Date(due), overdue, daysLate, fine), (loan.Due, loan.Overdue, loan.DaysLate, loan.Fine));
    }

    [Fact]
    public async Task Filters_combine_and_keep_id_order()
    {
        var ids = async (int? member, bool? active, bool? overdue) =>
            (await Loans.ListAsync(member, active, overdue, Today)).Select(l => l.Loan.Id).ToArray();

        Assert.Equal(new[] { 3, 4, 5, 6, 7, 8, 10, 11 }, await ids(null, true, null));
        Assert.Equal(new[] { 3, 4, 6, 8, 11 }, await ids(null, null, true));
        Assert.Equal(new[] { 5, 7, 10 }, await ids(null, true, false));
        Assert.Equal(new[] { 2, 8 }, await ids(102, null, null));
        Assert.Empty(await ids(999, null, null));
    }

    [Fact]
    public async Task A_members_loans_need_the_member_to_exist()
    {
        await Assert.ThrowsAsync<MemberNotFound>(() => Loans.ListForMemberAsync(999, null, null, Today));
        Assert.Equal(2, (await Loans.ListForMemberAsync(105, null, true, Today)).Count);
    }
}
