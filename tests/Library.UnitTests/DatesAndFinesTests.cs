using Library.Api.Data.Entities;
using Library.Api.Services;

namespace Library.UnitTests;

/// <summary>Due dates, overdue, days late and fines. Pure calculations: no database.</summary>
public class DatesAndFinesTests
{
    private static readonly DateOnly Today = new(2026, 10, 12);

    private static Loan LoanFor(string memberType, string borrowed, string? returned = null) => new()
    {
        Id = 1,
        BookId = 1,
        MemberId = 101,
        Member = new Member { Id = 101, Type = memberType },
        Borrowed = DateOnly.Parse(borrowed),
        Returned = returned is null ? null : DateOnly.Parse(returned),
    };

    [Theory]
    [InlineData("standard", "2026-10-12", "2026-10-26")]
    [InlineData("student", "2026-10-12", "2026-11-02")]
    [InlineData("staff", "2026-10-12", "2026-11-09")]
    [InlineData("student", "2026-12-20", "2027-01-10")] // crosses the year end
    [InlineData("standard", "2028-02-20", "2028-03-05")] // 2028 is a leap year
    public void Due_date_is_borrowed_plus_the_policy_loan_days(string type, string borrowed, string due)
    {
        Assert.Equal(DateOnly.Parse(due), LoanService.Describe(LoanFor(type, borrowed), Today).Due);
    }

    [Theory]
    [InlineData("standard", 14, 25, 3)]
    [InlineData("student", 21, 10, 5)]
    [InlineData("staff", 28, 0, 10)]
    public void Each_member_type_has_its_policy(string type, int loanDays, int finePerDay, int maxLoans)
    {
        Assert.Equal(new LoanPolicy(loanDays, finePerDay, maxLoans), LoanPolicy.For(type));
    }

    [Fact]
    public void Due_today_is_not_overdue_but_the_next_day_is()
    {
        var loan = LoanFor("student", "2026-09-21"); // loan 10, due 2026-10-12

        var onTheDay = LoanService.Describe(loan, Today);
        Assert.Equal((false, 0, 0), (onTheDay.Overdue, onTheDay.DaysLate, onTheDay.Fine));

        var dayAfter = LoanService.Describe(loan, Today.AddDays(1));
        Assert.Equal((true, 1, 10), (dayAfter.Overdue, dayAfter.DaysLate, dayAfter.Fine));
    }

    [Fact]
    public void One_day_late_standard_is_25p()
    {
        var loan = LoanService.Describe(LoanFor("standard", "2026-09-27"), Today); // due 2026-10-11
        Assert.Equal((true, 1, 25), (loan.Overdue, loan.DaysLate, loan.Fine));
    }

    [Fact]
    public void Ten_days_late_student_is_100p()
    {
        var loan = LoanService.Describe(LoanFor("student", "2026-09-11"), Today); // due 2026-10-02
        Assert.Equal((10, 100), (loan.DaysLate, loan.Fine));
    }

    [Fact]
    public void Staff_are_never_fined()
    {
        var loan = LoanService.Describe(LoanFor("staff", "2026-09-05"), new DateOnly(2027, 1, 1));
        Assert.Equal((true, 90, 0), (loan.Overdue, loan.DaysLate, loan.Fine));
    }

    [Fact]
    public void A_returned_loan_keeps_the_fine_from_its_return_date()
    {
        var loan = LoanFor("student", "2026-09-10", returned: "2026-10-05"); // loan 2, due 2026-10-01
        var later = LoanService.Describe(loan, new DateOnly(2027, 1, 1));
        Assert.Equal((false, 4, 40), (later.Overdue, later.DaysLate, later.Fine));
    }

    [Fact]
    public void Returned_early_is_zero_days_late()
    {
        var loan = LoanService.Describe(LoanFor("standard", "2026-09-01", returned: "2026-09-12"), Today);
        Assert.Equal((false, 0, 0), (loan.Overdue, loan.DaysLate, loan.Fine));
    }
}
