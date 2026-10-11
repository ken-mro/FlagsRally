namespace FlagsRally.Controls;

/// <summary>
/// Stamp label for a title bar that shows names from outside the app (board names) in full.
/// The bar cannot grow, so a long name wraps onto a second line and its font gets smaller
/// until the whole name fits the bar's height.
/// </summary>
public class FittingStampLabel : StampLabel
{
    public double MaxFontSize { get; set; } = 26;

    public double MinFontSize { get; set; } = 11;

    /// <summary>
    /// The height the text may take: iOS's navigation bar is 44 points, Android's toolbar 56.
    /// </summary>
    public double MaxTextHeight { get; set; } = 42;

    double _fittedWidth = -1;
    string? _fittedText;

    public FittingStampLabel()
    {
        LineBreakMode = LineBreakMode.WordWrap;
    }

    protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
    {
        if (double.IsFinite(widthConstraint) && widthConstraint > 0
            && (Math.Abs(widthConstraint - _fittedWidth) > 0.5 || _fittedText != Text))
        {
            _fittedWidth = widthConstraint;
            _fittedText = Text;
            FitFontSize(widthConstraint);
        }

        return base.MeasureOverride(widthConstraint, heightConstraint);
    }

    void FitFontSize(double width)
    {
        if (string.IsNullOrEmpty(Text) || Handler is null) return;

        for (var size = MaxFontSize; size >= MinFontSize; size--)
        {
            FontSize = size;
            if (base.MeasureOverride(width, double.PositiveInfinity).Height <= MaxTextHeight) return;
        }
        FontSize = MinFontSize;
    }
}
