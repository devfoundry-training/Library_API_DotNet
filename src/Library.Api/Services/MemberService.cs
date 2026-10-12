using System.Text.RegularExpressions;
using Library.Api.Data.Entities;
using Library.Api.Data.Repositories;

namespace Library.Api.Services;

/// <summary>The fields of a member as a client sends them. Joined may be left out on create.</summary>
public sealed record MemberData(string Name, string Email, string Type, DateOnly? Joined);

public sealed partial class MemberService(MemberRepository members)
{
    public Task<List<Member>> ListAsync() => members.ListAsync();

    public async Task<Member> GetAsync(int id) => await members.FindAsync(id) ?? throw new MemberNotFound(id);

    /// <summary>A new member joins today unless the client says otherwise.</summary>
    public async Task<Member> CreateAsync(MemberData data, DateOnly today)
    {
        Validate(data);
        await CheckEmailIsFreeAsync(data.Email, forMemberId: null);

        var member = new Member();
        Copy(data, member, data.Joined ?? today);
        members.Add(member);
        await members.SaveChangesAsync();
        return member;
    }

    /// <summary>Replaces every field. Keeping your own email is fine; another member's is not.</summary>
    public async Task<Member> UpdateAsync(int id, MemberData data)
    {
        Validate(data);
        var joined = data.Joined ?? throw new ValidationError("joined is required");
        var member = await members.FindAsync(id) ?? throw new MemberNotFound(id);
        await CheckEmailIsFreeAsync(data.Email, forMemberId: id);

        Copy(data, member, joined);
        await members.SaveChangesAsync();
        return member;
    }

    private async Task CheckEmailIsFreeAsync(string email, int? forMemberId)
    {
        var owner = await members.FindByEmailAsync(email);
        if (owner is not null && owner.Id != forMemberId)
        {
            throw new EmailTaken(email, owner.Id);
        }
    }

    private static void Copy(MemberData data, Member member, DateOnly joined)
    {
        member.Name = data.Name;
        member.Email = data.Email;
        member.Type = data.Type;
        member.Joined = joined;
    }

    private static void Validate(MemberData data)
    {
        Rules.Text("name", data.Name, maxLength: 100);
        if (data.Email.Length > 100 || !EmailShape().IsMatch(data.Email))
        {
            throw new ValidationError("email must look like name@example.com");
        }
        if (!LoanPolicy.IsMemberType(data.Type))
        {
            throw new ValidationError("member type must be standard, student or staff");
        }
    }

    // One @ with text on both sides and no spaces. \z, not $, so a trailing newline is refused.
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\z")]
    private static partial Regex EmailShape();
}
