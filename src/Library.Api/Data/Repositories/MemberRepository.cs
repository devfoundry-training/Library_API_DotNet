using Library.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Library.Api.Data.Repositories;

public sealed class MemberRepository(LibraryDbContext db)
{
    public Task<List<Member>> ListAsync() => db.Members.AsNoTracking().OrderBy(m => m.Id).ToListAsync();

    public Task<Member?> FindAsync(int id) => db.Members.SingleOrDefaultAsync(m => m.Id == id);

    public Task<bool> ExistsAsync(int id) => db.Members.AnyAsync(m => m.Id == id);

    /// <summary>The member using this exact email, if any (no case folding).</summary>
    public Task<Member?> FindByEmailAsync(string email) =>
        db.Members.AsNoTracking().SingleOrDefaultAsync(m => m.Email == email);

    public void Add(Member member) => db.Members.Add(member);

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}
