namespace FlagsRally.Models.CustomBoard;

public record CustomBoard
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
}
