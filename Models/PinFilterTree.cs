using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui;

namespace FlagsRally.Models;

/// <summary>
/// A pin key (a board's name, or the arrival location) and the series it sits in on the map's filter
/// (its outermost shared folder, or the series its name belongs to); null for none.
/// </summary>
public sealed record PinFilterEntry(string Key, string? Series);

/// <summary>
/// A row of the map's filter list, laid out like the Windows features dialog: a series of boards that
/// opens to show them (with a tick box for all, some or none of them) or a single board.
/// </summary>
public partial class PinFilterNode : ObservableObject
{
    const double IndentPerLevel = 18;

    PinFilterNode(string name, string? key, IReadOnlyList<PinFilterNode> children)
    {
        Name = name;
        Key = key;
        Children = children;
        Keys = key is not null ? [key] : children.SelectMany(c => c.Keys).ToList();
    }

    public static PinFilterNode Leaf(string key) => new(key, key, []);

    public static PinFilterNode Group(string name, IReadOnlyList<PinFilterNode> children) => new(name, null, children);

    public string Name { get; }

    /// <summary>The pin key of a single board; null for a group.</summary>
    public string? Key { get; }

    public IReadOnlyList<PinFilterNode> Children { get; }

    public bool IsGroup => Key is null;

    /// <summary>Every pin key under this row (its own for a board).</summary>
    public IReadOnlyList<string> Keys { get; }

    /// <summary>How deep the row sits in the list shown, 0 for the outermost rows.</summary>
    public int Depth { get; internal set; }

    // Indented by level, with room above and below so that names on two lines do not touch the next row.
    public Thickness Indent => new(Depth * IndentPerLevel, 4, 0, 4);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpandRotation))]
    bool _isExpanded;

    /// <summary>A group's ▸ turns to point down while it is open.</summary>
    public double ExpandRotation => IsExpanded ? 90 : 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAllShown))]
    [NotifyPropertyChangedFor(nameof(IsSomeShown))]
    [NotifyPropertyChangedFor(nameof(CountText))]
    int _shownCount;

    public PinsShown State => ShownCount == Keys.Count ? PinsShown.All : ShownCount == 0 ? PinsShown.None : PinsShown.Some;

    public bool IsAllShown => State == PinsShown.All;

    public bool IsSomeShown => State == PinsShown.Some;

    /// <summary>How many of a group's boards are shown, e.g. "3/31".</summary>
    public string CountText => IsGroup ? $"{ShownCount}/{Keys.Count}" : string.Empty;
}

public static class PinFilterTree
{
    /// <summary>
    /// The rows of the filter list, in the order the keys come in: each series in the place of its first
    /// board, with its boards inside. A series of a single board is shown as just the board.
    /// </summary>
    public static List<PinFilterNode> Build(IEnumerable<PinFilterEntry> entries)
    {
        var series = new Dictionary<string, List<string>>();
        var order = new List<(string? Series, string? Key)>();

        foreach (var (key, name) in entries)
        {
            var seriesName = name?.Trim();
            if (string.IsNullOrEmpty(seriesName))
            {
                order.Add((null, key));
                continue;
            }

            if (!series.TryGetValue(seriesName, out var keys))
            {
                series[seriesName] = keys = [];
                order.Add((seriesName, null));
            }
            keys.Add(key);
        }

        var rows = order
            .Select(o => o.Series is null
                ? PinFilterNode.Leaf(o.Key!)
                : series[o.Series] is [var only]
                    ? PinFilterNode.Leaf(only)
                    : PinFilterNode.Group(o.Series, series[o.Series].Select(PinFilterNode.Leaf).ToList()))
            .ToList();
        SetDepth(rows, 0);
        return rows;
    }

    static void SetDepth(IEnumerable<PinFilterNode> rows, int depth)
    {
        foreach (var row in rows)
        {
            row.Depth = depth;
            SetDepth(row.Children, depth + 1);
        }
    }

    /// <summary>The same rows rebuilt as the outermost level, such as a series' boards in its own list.</summary>
    public static List<PinFilterNode> Rebase(IEnumerable<PinFilterNode> rows)
    {
        var copies = rows.Select(Copy).ToList();
        SetDepth(copies, 0);
        return copies;
    }

    static PinFilterNode Copy(PinFilterNode row) =>
        row.IsGroup ? PinFilterNode.Group(row.Name, row.Children.Select(Copy).ToList()) : PinFilterNode.Leaf(row.Key!);

    /// <summary>Counts each row's shown boards, for its tick box.</summary>
    public static void Refresh(IEnumerable<PinFilterNode> rows, IReadOnlyCollection<string> hiddenKeys)
    {
        foreach (var row in rows)
        {
            row.ShownCount = row.Keys.Count(k => !hiddenKeys.Contains(k));
            Refresh(row.Children, hiddenKeys);
        }
    }

    /// <summary>
    /// The rows on screen: the outermost ones and those inside opened groups, in order.
    /// </summary>
    public static IEnumerable<PinFilterNode> Visible(IEnumerable<PinFilterNode> rows)
    {
        foreach (var row in rows)
        {
            yield return row;
            if (!row.IsExpanded) continue;
            foreach (var child in Visible(row.Children)) yield return child;
        }
    }

    /// <summary>
    /// Whether a tap on a row's tick box shows its boards: as in the Windows features dialog, a group with
    /// only some shown is filled in completely, and one with all shown is cleared.
    /// </summary>
    public static bool ShowsOnTap(PinFilterNode row) => row.State != PinsShown.All;
}
