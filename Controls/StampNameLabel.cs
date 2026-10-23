using FlagsRally.Helpers;

namespace FlagsRally.Controls;

/// <summary>
/// A place name on a passport stamp. It gets smaller to stay on one line; when even
/// <see cref="MinFontSize"/> is too large it goes onto two even lines, broken where
/// <see cref="NameLineBreak"/> says, and gets smaller again down to <see cref="TwoLineMinFontSize"/>.
/// A Japanese name with nowhere good to break (no space or "・") stays on one line down to that
/// smaller size first. Past it the two lines keep shrinking down to <see cref="LastFontSize"/>, so
/// the whole name always shows, on two lines.
/// Bind the name to <see cref="Name"/>, not Text, which the label sets itself.
/// </summary>
public class StampNameLabel : Label
{
    public static readonly BindableProperty NameProperty = BindableProperty.Create(
        nameof(Name), typeof(string), typeof(StampNameLabel), null,
        propertyChanged: (view, _, value) => ((StampNameLabel)view).NameChanged((string?)value));

    public string? Name
    {
        get => (string?)GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }

    /// <summary>The smallest size for one line, before going onto two.</summary>
    public double MinFontSize { get; set; } = 11;

    public double TwoLineMinFontSize { get; set; } = 8;

    /// <summary>
    /// False keeps the name on one line, shrinking down to <see cref="LastFontSize"/>; only a name
    /// too long even at that size goes onto two lines, still broken between words.
    /// </summary>
    public static readonly BindableProperty AllowTwoLinesProperty = BindableProperty.Create(
        nameof(AllowTwoLines), typeof(bool), typeof(StampNameLabel), true,
        propertyChanged: (view, _, _) => ((StampNameLabel)view).NameChanged(((StampNameLabel)view).Name));

    public bool AllowTwoLines
    {
        get => (bool)GetValue(AllowTwoLinesProperty);
        set => SetValue(AllowTwoLinesProperty, value);
    }

    /// <summary>How small two lines may get for the longest names, rather than a third line.</summary>
    public double LastFontSize { get; set; } = 6;

    // The size set in XAML, so a recycled cell starts again from it for a shorter name.
    // A width within 2 points of the last one keeps the fit, and so does a wider one the name already
    // fits: a popup sized by its content measures it at two widths, and refitting at each would
    // change its size, then the widths, and lay it out again without end.
    double _maxFontSize = -1;
    double _fittedWidth = -1;

    public StampNameLabel()
    {
        LineBreakMode = LineBreakMode.WordWrap;
    }

    void NameChanged(string? name)
    {
        _fittedWidth = -1;
        Text = name;
        InvalidateMeasure();
    }

    protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
    {
        if (double.IsFinite(widthConstraint) && widthConstraint > 0 && Math.Abs(widthConstraint - _fittedWidth) > 2
            && !(widthConstraint > _fittedWidth && _fittedWidth > 0 && FitsAsIs(widthConstraint)))
        {
            _fittedWidth = widthConstraint;
            Fit(widthConstraint);
        }
        return base.MeasureOverride(widthConstraint, heightConstraint);
    }

    void Fit(double width)
    {
        var name = Name;
        if (string.IsNullOrEmpty(name) || Handler is null) return;
        if (_maxFontSize < 0) _maxFontSize = FontSize;

        Text = name;
        var split = NameLineBreak.SplitInTwo(name);
        if (!AllowTwoLines)
        {
            // One line down to the smallest size; only a name too long even for that takes two
            if (Shrink(_maxFontSize, LastFontSize, width) || split is null) return;
            Text = $"{split.Value.First}\n{split.Value.Second}";
            Shrink(LastFontSize, LastFontSize, width);
            return;
        }

        // One line; a name that cannot be broken well keeps shrinking on its line
        var smallest = split is null or { InWord: true } ? TwoLineMinFontSize : MinFontSize;
        if (Shrink(_maxFontSize, smallest, width)) return;
        if (split is null)
        {
            Shrink(TwoLineMinFontSize, LastFontSize, width);
            return;
        }

        Text = $"{split.Value.First}\n{split.Value.Second}";
        Shrink(Math.Min(MinFontSize, _maxFontSize), LastFontSize, width);
    }

    bool FitsAsIs(double width) =>
        Handler is not null && base.MeasureOverride(double.PositiveInfinity, double.PositiveInfinity).Width <= width;

    // Whether the text fits the width at some size from largest down to smallest; left at smallest if not
    bool Shrink(double largest, double smallest, double width)
    {
        for (var size = largest; size >= smallest; size -= 0.5)
        {
            FontSize = size;
            if (base.MeasureOverride(double.PositiveInfinity, double.PositiveInfinity).Width <= width) return true;
        }
        FontSize = smallest;
        return false;
    }
}
