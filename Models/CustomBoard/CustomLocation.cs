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
    public bool HasBeenVisited => ArrivalDate is not null;
    public bool HasNotBeenVisited => !HasBeenVisited;
    public string ArrivalDateString => ArrivalDate?.ToString("dd  MMM  yyyy", CultureInfo.CreateSpecificCulture("en-US")) ?? string.Empty;

    private string GetImageUrl()
    {
        var matches = Regex.Matches(Board.Url, @"\{(\w+)\}");
        var url = Board.Url;
        foreach (Match match in matches)
        {
            var propertyName = match.Groups[1].Value;
            var pascalCasePropertyName = char.ToUpper(propertyName[0]) + propertyName.Substring(1);
            var property = this.GetType().GetProperty(pascalCasePropertyName);
            var value = property?.GetValue(this)?.ToString();

            url = url.Replace($"{{{propertyName}}}", value);
        }
        return url;
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