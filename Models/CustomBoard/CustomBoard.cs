namespace FlagsRally.Models.CustomBoard;

public record CustomBoard
{
    public string Name { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public int Width { get; init; } = 0;
    public int Height { get; init; } = 0;
}
