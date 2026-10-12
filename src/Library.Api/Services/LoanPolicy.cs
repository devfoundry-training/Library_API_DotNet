namespace Library.Api.Services;

/// <summary>
/// The loan policy for each member type (task 06). Fines are whole pence per day late.
/// </summary>
public sealed record LoanPolicy(int LoanDays, int FinePerDay, int MaxLoans)
{
    public static readonly LoanPolicy Standard = new(LoanDays: 14, FinePerDay: 25, MaxLoans: 3);
    public static readonly LoanPolicy Student = new(LoanDays: 21, FinePerDay: 10, MaxLoans: 5);
    public static readonly LoanPolicy Staff = new(LoanDays: 28, FinePerDay: 0, MaxLoans: 10);

    private static readonly Dictionary<string, LoanPolicy> ByType = new(StringComparer.Ordinal)
    {
        ["standard"] = Standard,
        ["student"] = Student,
        ["staff"] = Staff,
    };

    public static bool IsMemberType(string type) => ByType.ContainsKey(type);

    public static LoanPolicy For(string memberType) =>
        ByType.TryGetValue(memberType, out var policy)
            ? policy
            : throw new ArgumentException($"unknown member type '{memberType}'", nameof(memberType));

    public DateOnly DueDate(DateOnly borrowed) => borrowed.AddDays(LoanDays);
}
