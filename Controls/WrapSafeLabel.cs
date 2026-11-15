namespace FlagsRally.Controls;

/// <summary>
/// A wrapping label whose height always has room for the line its text wraps onto.
/// Android can measure a text as just fitting on one line and then, laid out a few pixels
/// narrower, wrap its end onto a second line that the measured height cuts off: on a Galaxy
/// Z Fold cover screen the map's "Not visited" showed without its count (four digits, such
/// as 5000). Measuring a little narrower than offered makes the label ask for the second line
/// whenever the text may wrap; when it then fits on one line, it is only centred in a taller row.
/// </summary>
public class WrapSafeLabel : Label
{
    const double Slack = 2;

    protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
    {
        if (!double.IsFinite(widthConstraint) || widthConstraint <= Slack * 2)
        {
            return base.MeasureOverride(widthConstraint, heightConstraint);
        }

        var size = base.MeasureOverride(widthConstraint - Slack, heightConstraint);
        return new Size(Math.Min(widthConstraint, size.Width + Slack), size.Height);
    }
}
