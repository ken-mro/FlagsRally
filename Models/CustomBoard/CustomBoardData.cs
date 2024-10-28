using SQLite;

namespace FlagsRally.Models.CustomBoard;

[Table("CustomBoard")]
public class CustomBoardData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    [MaxLength(168)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string Url { get; set; } = string.Empty;

    public int Width { get; set; }
    public int Height { get; set; }
    
    public CustomBoardData() { } // Required for SQLite
}
