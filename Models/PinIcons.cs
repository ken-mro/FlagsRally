using FlagsRally.Models.CustomBoard;
using Maui.GoogleMaps;

namespace FlagsRally.Models;

/// <summary>
/// The look of the map pins, chosen in Settings.
/// </summary>
public enum PinStyle
{
    /// <summary>The original push pins, redrawn flat (default).</summary>
    Classic,
    /// <summary>Teardrop pins like the one in the app icon.</summary>
    Drop,
}

public enum PinKind { Unvisited, Visited, CheckIn, CheckInSpot }

public static class PinIcons
{
    /// <summary>
    /// The style used for new pins; set from Settings at start-up and when it is changed.
    /// </summary>
    public static PinStyle Style { get; set; } = PinStyle.Classic;

    /// <summary>
    /// The bundled image for a kind of pin in a style (see Resources/Images).
    /// </summary>
    public static string FileName(PinKind kind, PinStyle style)
    {
        var name = kind switch
        {
            PinKind.Visited => "pin_arrived",
            PinKind.CheckIn => "default_pin",
            PinKind.CheckInSpot => "selected_location_pin",
            _ => "pin",
        };
        return style == PinStyle.Classic ? $"classic_{name}" : name;
    }

    public static BitmapDescriptor For(PinKind kind) => BitmapDescriptorFactory.FromBundle(FileName(kind, Style));

    /// <summary>
    /// The icon a pin on the map should have in the current style, or null for pins this app did not make.
    /// </summary>
    public static BitmapDescriptor? ForPin(Pin pin) => pin switch
    {
        CustomLocationPin custom => For(custom.IsVisited ? PinKind.Visited : PinKind.Unvisited),
        ArrivalLocationPin => For(PinKind.CheckIn),
        SelectedLocationPin => For(PinKind.CheckInSpot),
        _ => null,
    };
}
