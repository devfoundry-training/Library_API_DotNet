using Library.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Data.Repositories;

/// <summary>Loans are always loaded with their member, because the member type decides the policy.</summary>
public sealed class LoanRepository(LibraryDbContext db)
{
    /// <summary>
    /// Loans in id order. memberId keeps one member's loans; active true keeps open loans, false
    /// keeps returned loans. A null filter is not applied.
    /// </summary>
    public Task<List<Loan>> ListAsync(int? memberId, bool? active)
    {
        var loans = db.Loans.AsNoTracking().Include(l => l.Member).AsQueryable();
        if (memberId is not null)
        {
            loans = loans.Where(l => l.MemberId == memberId);
        }
        if (active is not null)
        {
            loans = active.Value ? loans.Where(l => l.Returned == null) : loans.Where(l => l.Returned != null);
        }
        return loans.OrderBy(l => l.Id).ToListAsync();
    }

    public Task<Loan?> FindAsync(int id) =>
        db.Loans.Include(l => l.Member).SingleOrDefaultAsync(l => l.Id == id);

    public Task<int> CountOpenForMemberAsync(int memberId) =>
        db.Loans.CountAsync(l => l.MemberId == memberId && l.Returned == null);

    public void Add(Loan loan) => db.Loans.Add(loan);

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
