namespace FlagsRally.Models.CustomBoard;

public class CustomLocationPin
{
    public int Id { get; init; }
    public int BoardId { get; init; }
    public string Title { get; init; } = string.Empty;
    public Location PinLocation { get; init; } = new();
    public bool IsVisited { get; set; }
}
