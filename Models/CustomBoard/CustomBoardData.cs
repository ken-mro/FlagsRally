using SQLite;

namespace FlagsRally.Models.CustomBoard;

[Table("CustomBoard")]
public class CustomBoardData
{
    [PrimaryKey, MaxLength(168)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string Url { get; set; } = string.Empty;

    public int Width { get; set; }
    public int Height { get; set; }
}
