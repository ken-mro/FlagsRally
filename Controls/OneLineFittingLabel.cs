namespace FlagsRally.Controls;

/// <summary>
/// A label kept on one line by making its font smaller when the text would not fit the width,
/// such as a stamp's date with large text on iOS ("09 10月 2026" broke after the month).
/// The whole text stays visible; below <see cref="MinFontSize"/> it wraps as a plain label does.
/// </summary>
public class OneLineFittingLabel : Label
{
    public double MinFontSize { get; set; } = 9;

    // The size set in XAML, so a recycled cell starts again from it for a shorter text.
    double _maxFontSize = -1;
    double _fittedWidth = -1;
    string? _fittedText;

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
        if (_maxFontSize < 0) _maxFontSize = FontSize;

        for (var size = _maxFontSize; size >= MinFontSize; size -= 0.5)
        {
            FontSize = size;
            if (base.MeasureOverride(double.PositiveInfinity, double.PositiveInfinity).Width <= width) return;
        }
        FontSize = MinFontSize;
    }
}
