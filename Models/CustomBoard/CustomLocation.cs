using CommunityToolkit.Mvvm.ComponentModel;
using FlagsRally.Helpers;
using System.Text.RegularExpressions;

namespace FlagsRally.Models.CustomBoard;

public class CustomLocation : ObservableObject
{
    public string CompositeKey => $"{Board.Name}-{Code}";
    public CustomBoard Board { get; init; } = new();
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string Group { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
    /// <summary>
    /// The stamp to show on the board: only a visited place has one, so an unvisited place's image is never fetched.
    /// A pending stamp is not shown either, so it is first seen pressed down with its celebration.
    /// </summary>
    public string? StampImageUrl => HasBeenVisited && !StampUnseen && PressedStamp is null ? ImageUrl : null;
    public Location Location { get; init; } = new ();
    public DateTime? ArrivalDate { get; init; } = null;
    /// <summary>
    /// 1-based position in the board's JSON.
    /// </summary>
    public int SortIndex { get; init; }
    /// <summary>
    /// Checked in without its stamp's image; the celebration waits until the stamp is first looked at.
    /// </summary>
    public bool StampUnseen { get; init; }
    /// <summary>
    /// Checked in, with the stamp still waiting to be pressed down when it is first opened.
    /// </summary>
    public bool IsStampPending => HasBeenVisited && StampUnseen && PressedStamp is null;

    /// <summary>
    /// A pending stamp downloaded and pressed down on its tile just now: shown from these bytes (not
    /// fetched again by URL) until the board is loaded again.
    /// </summary>
    public ImageSource? PressedStamp { get; private set; }

    bool _pressAnimationPending;

    /// <summary>
    /// Shows a pending stamp, just downloaded, on its tile, pressed down there with its celebration.
    /// </summary>
    public void PressStamp(byte[] image)
    {
        _pressAnimationPending = true;
        PressedStamp = ImageSource.FromStream(() => new MemoryStream(image));
        OnPropertyChanged(nameof(PressedStamp));
        OnPropertyChanged(nameof(IsStampPending));
        OnPropertyChanged(nameof(StampImageUrl));
    }

    /// <summary>
    /// Whether the tile still has to play the press for <see cref="PressStamp"/>; true only once, so a
    /// tile scrolled back into view or reused for another place does not press it again.
    /// </summary>
    public bool TakePressAnimation()
    {
        var pending = _pressAnimationPending;
        _pressAnimationPending = false;
        return pending;
    }
    public bool HasBeenVisited => ArrivalDate is not null;
    public bool HasNotBeenVisited => !HasBeenVisited;
    public string ArrivalDateString => ArrivalDate is DateTime date ? StampDate.FormatSpaced(date) : string.Empty;

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