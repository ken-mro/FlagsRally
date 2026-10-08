using SQLite;

namespace FlagsRally.Models.CustomBoard;

[Table("CustomLocation")]
public class CustomLocationData
{
    [PrimaryKey, MaxLength(168)]
    public string CompositeKey { get; init; } = string.Empty;
    public string BoardName { get; init; } = string.Empty;
    
    [MaxLength(168)]
    public string Code { get; init; } = string.Empty;

    [MaxLength(168)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(168)]
    public string Subtitle { get; init; } = string.Empty;

    [MaxLength(168)]
    public string Group { get; init; } = string.Empty;

    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public DateTime? ArrivalDate { get; set; } = null;

    /// <summary>
    /// 1-based position in the board's JSON; rows saved before the order was recorded get their rowid.
    /// </summary>
    public int SortIndex { get; set; }
}
