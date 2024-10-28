namespace FlagsRally.Models.CustomBoard;

public record CustomLocation
{
    public int Id { get; init; }
    public CustomBoard Board { get; init; } = new();
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string Group { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
    public Location Location { get; init; } = new ();
    public DateTime? ArrivalDate { get; init; } = null;
    public bool HasBeenVisited => ArrivalDate is not null;
    public bool HasNotBeenVisited => !HasBeenVisited;
    public string ArrivalDateString => ArrivalDate?.ToString("dd MMM yyyy") ?? string.Empty;
}
