namespace Library.Api.Today;

/// <summary>"What is today" for the current request. Services never read the clock: they are given this date.</summary>
public interface IToday
{
    DateOnly Value { get; }
}
