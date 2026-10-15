using FlagsRally.Models;
using Maui.GoogleMaps;

namespace FlagsRally.Helpers;

public static class PinOverlapHelper
{
    public const double DEFAULT_TOLERANCE_METERS = 1d;

    /// <summary>
    /// Returns the visible pins located at (practically) the same coordinates as <paramref name="tappedPin"/>,
    /// in the order of <paramref name="pins"/>, so the list is the same whichever of them was tapped.
    /// Returns just the tapped pin when nothing overlaps it.
    /// </summary>
    public static IReadOnlyList<Pin> FindOverlapping(Pin tappedPin, IEnumerable<Pin> pins, double toleranceMeters = DEFAULT_TOLERANCE_METERS)
    {
        var tappedLocation = ToLocation(tappedPin.Position);
        // Pin equality is value-based (same label and position compare equal), so compare references.
        var overlapping = pins.Where(p => ReferenceEquals(p, tappedPin)
                                          || (p.IsVisible && tappedLocation.CalculateDistance(ToLocation(p.Position), DistanceUnits.Kilometers) * 1000 <= toleranceMeters))
                              .ToList();
        return overlapping.Count > 0 ? overlapping : [tappedPin];
    }

    /// <summary>
    /// Builds a distinct display label for each pin (title and board name), for use in a chooser.
    /// </summary>
    public static IReadOnlyList<string> GetChoiceLabels(IReadOnlyList<Pin> pins)
    {
        var labels = new List<string>(pins.Count);
        foreach (var pin in pins)
        {
            var label = pin.Tag is MapPinTag tag ? $"{pin.Label} – {tag.BoardName}" : pin.Label;
            var uniqueLabel = label;
            for (int i = 2; labels.Contains(uniqueLabel); i++)
            {
                uniqueLabel = $"{label} ({i})";
            }
            labels.Add(uniqueLabel);
        }
        return labels;
    }

    private static Location ToLocation(Position position) => new(position.Latitude, position.Longitude);
}
