namespace FlagsRally.Models.CustomBoard;

public class CustomBoardJson
{
    public string name { get; set; } = string.Empty;
    public string url { get; set; } = string.Empty;
    public int width { get; set; }
    public int height { get; set; }
    public CustomBoardLocationJson[] locations { get; set; } = [];
}

public class CustomBoardLocationJson
{
    public string code { get; set; } = string.Empty;
    public string title { get; set; } = string.Empty;
    public string subtitle { get; set; } = string.Empty;
    public string group { get; set; } = string.Empty;
    public double latitude { get; set; }
    public double longitude { get; set; }
}