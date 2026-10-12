namespace Library.Api.Data.Entities;

public class Member
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Type { get; set; } = "";
    public DateOnly Joined { get; set; }

    public List<Loan> Loans { get; } = [];
}
