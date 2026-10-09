using System.Globalization;
using System.Text.RegularExpressions;

namespace FlagsRally.Models.CustomBoard;

public class CustomLocation
{
    public string CompositeKey => $"{Board.Name}-{Code}";
    public CustomBoard Board { get; init; } = new();
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string Group { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
    public Location Location { get; init; } = new ();
    public DateTime? ArrivalDate { get; init; } = null;
    /// <summary>
    /// 1-based position in the board's JSON.
    /// </summary>
    public int SortIndex { get; init; }
    public bool HasBeenVisited => ArrivalDate is not null;
    public bool HasNotBeenVisited => !HasBeenVisited;
    public string ArrivalDateString => ArrivalDate?.ToString("dd  MMM  yyyy", CultureInfo.CreateSpecificCulture("en-US")) ?? string.Empty;

    // Boards come from other people, so their image URL may only use the place's own descriptive
    // fields; anything else (visit dates in particular) must never reach the image server.
    private string GetImageUrl()
    {
        return Regex.Replace(Board.Url, @"\{(\w+)\}", match =>
        {
            string? value = match.Groups[1].Value.ToLowerInvariant() switch
            {
                "code" => Code,
                "title" => Title,
                "subtitle" => Subtitle,
                "group" => Group,
                _ => null,
            };
            return value is null ? string.Empty : Uri.EscapeDataString(value);
        });
    }
    public CustomLocation(CustomBoard board, string code, string title, string subtitle, string group, Location location, DateTime? arrivalDate)
    {
        Board = board;   
        Code = code;
        Title = title;
        Subtitle = subtitle;
        Group = group;
        Location = location;
        ArrivalDate = arrivalDate;
        ImageUrl = GetImageUrl();
    }
}