using Microsoft.Maui.Layouts;

namespace FlagsRally.Controls;

/// <summary>
/// Lays out its one child at least <see cref="DesignWidth"/> wide and, in a narrower space, shrinks
/// it as a whole to fit, as a picture would. With <see cref="MaxScale"/> above 1 it also grows it in a
/// wider space, up to that scale (the stamp in the View stamp popup). A passport stamp keeps its shape and its text its
/// lines in the narrow grid cells of a small phone at its largest display size (two columns of
/// about 156 points), where it would otherwise grow tall and squeeze its name and date.
/// </summary>
public class ShrinkToFitLayout : Layout
{
    public double DesignWidth { get; set; } = 180;

    public double MaxScale { get; set; } = 1;

    protected override ILayoutManager CreateLayoutManager() => new Manager(this);

    sealed class Manager(ShrinkToFitLayout layout) : ILayoutManager
    {
        double _width;
        double _scale = 1;
        Size _childSize;

        public Size Measure(double widthConstraint, double heightConstraint)
        {
            if (layout.Count == 0) return Size.Zero;
            var child = layout[0];
            // The scale that fits the design to the width, at most MaxScale; the child fills the rest
            _scale = double.IsFinite(widthConstraint) ? Math.Min(layout.MaxScale, widthConstraint / layout.DesignWidth) : 1;
            _width = double.IsFinite(widthConstraint) ? widthConstraint / _scale : layout.DesignWidth;
            _childSize = child.Measure(_width, double.PositiveInfinity);
            var width = double.IsFinite(widthConstraint) ? widthConstraint : _childSize.Width;
            return new Size(width, _childSize.Height * _scale);
        }

        public Size ArrangeChildren(Rect bounds)
        {
            if (layout.Count == 0) return bounds.Size;
            var child = layout[0];
            // Arranged at its full size round the middle; the scale shrinks it about its centre
            child.Arrange(new Rect(
                bounds.Center.X - _width / 2, bounds.Center.Y - _childSize.Height / 2, _width, _childSize.Height));
            if (child is VisualElement element && Math.Abs(element.Scale - _scale) > 0.001) element.Scale = _scale;
            return bounds.Size;
        }
    }
}
