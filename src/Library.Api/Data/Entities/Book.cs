namespace Library.Api.Data.Entities;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public int Year { get; set; }
    public string Genre { get; set; } = "";
    public int Copies { get; set; }
    public string Format { get; set; } = "";
    public int Length { get; set; }

    public List<Loan> Loans { get; } = [];
}
