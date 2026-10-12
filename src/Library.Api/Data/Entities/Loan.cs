namespace Library.Api.Data.Entities;

/// <summary>
/// Only what is stored. Due date, days late, fine and overdue are worked out by the service layer.
/// </summary>
public class Loan
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public int MemberId { get; set; }
    public DateOnly Borrowed { get; set; }
    public DateOnly? Returned { get; set; }

    public Book Book { get; set; } = null!;
    public Member Member { get; set; } = null!;
}
