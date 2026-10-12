using Maui.GoogleMaps;

namespace FlagsRally.Helpers;

/// <summary>
/// Tilts pins that would cover each other even at the closest zoom, so they fan out like a hand of
/// cards: each tip stays on its own spot and the heads point apart.
/// </summary>
public static class PinFanHelper
{
    /// <summary>
    /// About the width of a pin (32 dp) at Google Maps' closest zoom; spots nearer than this stay covered.
    /// </summary>
    public const double DEFAULT_TOLERANCE_METERS = 3d;

    /// <summary>The tilt between neighbouring pins of a small group.</summary>
    public const double MaxStepDegrees = 35d;

    /// <summary>The widest a group is fanned, from the leftmost pin to the rightmost.</summary>
    public const double MaxSpreadDegrees = 140d;

    const double MetersPerDegree = 111_320d;

    /// <summary>
    /// Sets the rotation of the visible pins: pins grouped with others are fanned, the rest stand upright.
    /// Pins that can be dragged are left upright and out of the groups.
    /// </summary>
    public static void Apply(IEnumerable<Pin> pins, double toleranceMeters = DEFAULT_TOLERANCE_METERS)
    {
        var fanned = new List<Pin>();
        foreach (var pin in pins)
        {
            if (pin.IsVisible && !pin.IsDraggable)
            {
                fanned.Add(pin);
            }
            else
            {
                pin.Rotation = 0f;
            }
        }

        var rotations = GetRotations(fanned.Select(p => p.Position).ToList(), toleranceMeters);
        for (int i = 0; i < fanned.Count; i++)
        {
            fanned[i].Rotation = (float)rotations[i];
        }
    }

    /// <summary>
    /// The clockwise tilt in degrees for each position. Positions within <paramref name="toleranceMeters"/>
    /// of each other (directly or through a chain) form a group, spread evenly around upright from west to
    /// east, so the westernmost leans left. A position alone gets 0.
    /// </summary>
    public static double[] GetRotations(IReadOnlyList<Position> positions, double toleranceMeters = DEFAULT_TOLERANCE_METERS)
    {
        var rotations = new double[positions.Count];
        if (positions.Count < 2 || toleranceMeters <= 0) return rotations;

        // Local metres (equirectangular), plenty accurate over a few metres.
        var points = positions.Select(p => (X: p.Longitude * MetersPerDegree * Math.Cos(p.Latitude * Math.PI / 180), Y: p.Latitude * MetersPerDegree)).ToArray();

        var parent = Enumerable.Range(0, points.Length).ToArray();
        int Find(int i) => parent[i] == i ? i : parent[i] = Find(parent[i]);

        // Only pins in the same or a neighbouring cell can be close enough, so this stays fast with thousands of pins.
        var cells = new Dictionary<(long, long), List<int>>();
        for (int i = 0; i < points.Length; i++)
        {
            var cx = (long)Math.Floor(points[i].X / toleranceMeters);
            var cy = (long)Math.Floor(points[i].Y / toleranceMeters);
            for (long dx = -1; dx <= 1; dx++)
            {
                for (long dy = -1; dy <= 1; dy++)
                {
                    if (!cells.TryGetValue((cx + dx, cy + dy), out var neighbours)) continue;
                    foreach (var j in neighbours)
                    {
                        var distX = points[i].X - points[j].X;
                        var distY = points[i].Y - points[j].Y;
                        if (distX * distX + distY * distY <= toleranceMeters * toleranceMeters)
                        {
                            parent[Find(i)] = Find(j);
                        }
                    }
                }
            }
            if (!cells.TryGetValue((cx, cy), out var cell))
            {
                cells[(cx, cy)] = cell = [];
            }
            cell.Add(i);
        }

        foreach (var group in Enumerable.Range(0, points.Length).GroupBy(Find).Where(g => g.Count() > 1))
        {
            // West to east, then north to south; pins on the very same spot keep their order.
            var members = group.OrderBy(i => points[i].X).ThenByDescending(i => points[i].Y).ThenBy(i => i).ToList();
            var step = Math.Min(MaxStepDegrees, MaxSpreadDegrees / (members.Count - 1));
            for (int k = 0; k < members.Count; k++)
            {
                rotations[members[k]] = (k - (members.Count - 1) / 2d) * step;
            }
        }
        return rotations;
    }
}
