using SQLite;

namespace FlagsRally.Models.CustomBoard;

[Table("CustomLocation")]
public class CustomLocationData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int BoardId { get; init; }
    
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
    public DateTime? ArrivalDate { get; init; } = null;
}
