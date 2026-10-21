using FlagsRally.Models;

namespace FlagsRally.Controls;

/// <summary>
/// The outline of a passport stamp, drawn to fill its bounds: a solid line with a dashed one inside,
/// in the shape and ink of the pattern. The stamp's text sits on top of it.
/// One view per stamp, so a long passport stays as quick to scroll as with plain borders.
/// </summary>
public class StampFrame : GraphicsView
{
    public static readonly BindableProperty PatternProperty = BindableProperty.Create(
        nameof(Pattern), typeof(StampPattern), typeof(StampFrame), StampPattern.Square,
        propertyChanged: (view, _, _) => ((StampFrame)view).Invalidate());

    /// <summary>Thinner lines for the small previews in Settings.</summary>
    public static readonly BindableProperty IsSmallProperty = BindableProperty.Create(
        nameof(IsSmall), typeof(bool), typeof(StampFrame), false,
        propertyChanged: (view, _, _) => ((StampFrame)view).Invalidate());

    public StampFrame()
    {
        Drawable = new FrameDrawable(this);
        BackgroundColor = Colors.Transparent;
        InputTransparent = true;
    }

    public StampPattern Pattern
    {
        get => (StampPattern)GetValue(PatternProperty);
        set => SetValue(PatternProperty, value);
    }

    public bool IsSmall
    {
        get => (bool)GetValue(IsSmallProperty);
        set => SetValue(IsSmallProperty, value);
    }

    sealed class FrameDrawable(StampFrame frame) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF bounds)
        {
            var small = frame.IsSmall;
            var outer = small ? 1.6f : 3.5f;
            var inner = small ? 0.8f : 1.5f;
            var gap = small ? 2.5f : 5f;
            var ink = StampPatterns.InkFor(frame.Pattern);

            canvas.StrokeColor = ink;
            canvas.FillColor = ink.WithAlpha(0.05f);

            var outerRect = bounds.Inflate(-outer, -outer);
            var innerRect = outerRect.Inflate(-gap, -gap);

            switch (frame.Pattern)
            {
                case StampPattern.Circle:
                case StampPattern.Oval:
                    if (frame.Pattern == StampPattern.Circle)
                    {
                        // A true circle, however tall or wide the stamp is
                        var size = Math.Min(outerRect.Width, outerRect.Height);
                        outerRect = new RectF(outerRect.Center.X - size / 2, outerRect.Center.Y - size / 2, size, size);
                        innerRect = outerRect.Inflate(-gap, -gap);
                    }
                    canvas.FillEllipse(outerRect);
                    Stroke(canvas, outer, () => canvas.DrawEllipse(outerRect));
                    Dashed(canvas, inner, () => canvas.DrawEllipse(innerRect));
                    break;
                case StampPattern.Octagon:
                    canvas.FillPath(Octagon(outerRect));
                    Stroke(canvas, outer, () => canvas.DrawPath(Octagon(outerRect)));
                    Dashed(canvas, inner, () => canvas.DrawPath(Octagon(innerRect)));
                    break;
                case StampPattern.Postage:
                    var edge = Perforated(outerRect, small ? 3f : 6f);
                    canvas.FillPath(edge);
                    Stroke(canvas, inner, () => canvas.DrawPath(edge));
                    Stroke(canvas, outer * 0.7f, () => canvas.DrawRectangle(innerRect.Inflate(-gap * 0.4f, -gap * 0.4f)));
                    break;
                default:
                    Stroke(canvas, outer, () => canvas.DrawRectangle(outerRect));
                    Dashed(canvas, inner, () => canvas.DrawRectangle(innerRect));
                    break;
            }
        }

        static void Stroke(ICanvas canvas, float width, Action draw)
        {
            canvas.StrokeSize = width;
            canvas.StrokeDashPattern = null;
            draw();
        }

        static void Dashed(ICanvas canvas, float width, Action draw)
        {
            canvas.StrokeSize = width;
            canvas.StrokeDashPattern = [2, 2];
            draw();
            canvas.StrokeDashPattern = null;
        }

        // Corners cut off, the same amount on every side.
        static PathF Octagon(RectF r)
        {
            var c = Math.Min(r.Width, r.Height) * 0.24f;
            var path = new PathF();
            path.MoveTo(r.Left + c, r.Top);
            path.LineTo(r.Right - c, r.Top);
            path.LineTo(r.Right, r.Top + c);
            path.LineTo(r.Right, r.Bottom - c);
            path.LineTo(r.Right - c, r.Bottom);
            path.LineTo(r.Left + c, r.Bottom);
            path.LineTo(r.Left, r.Bottom - c);
            path.LineTo(r.Left, r.Top + c);
            path.Close();
            return path;
        }

        // A postage stamp's edge: little half-round bites all the way round.
        static PathF Perforated(RectF r, float step)
        {
            var path = new PathF();
            var depth = step * 0.45f;
            void Edge(PointF from, PointF to, PointF inward)
            {
                var length = MathF.Sqrt((to.X - from.X) * (to.X - from.X) + (to.Y - from.Y) * (to.Y - from.Y));
                var count = Math.Max(1, (int)MathF.Round(length / step));
                for (var i = 0; i < count; i++)
                {
                    var t0 = (float)i / count;
                    var t1 = (float)(i + 1) / count;
                    var mid = (t0 + t1) / 2;
                    var end = new PointF(from.X + (to.X - from.X) * t1, from.Y + (to.Y - from.Y) * t1);
                    var control = new PointF(from.X + (to.X - from.X) * mid + inward.X * depth * 2,
                                             from.Y + (to.Y - from.Y) * mid + inward.Y * depth * 2);
                    path.QuadTo(control, end);
                }
            }
            path.MoveTo(r.Left, r.Top);
            Edge(new PointF(r.Left, r.Top), new PointF(r.Right, r.Top), new PointF(0, 1));
            Edge(new PointF(r.Right, r.Top), new PointF(r.Right, r.Bottom), new PointF(-1, 0));
            Edge(new PointF(r.Right, r.Bottom), new PointF(r.Left, r.Bottom), new PointF(0, -1));
            Edge(new PointF(r.Left, r.Bottom), new PointF(r.Left, r.Top), new PointF(1, 0));
            path.Close();
            return path;
        }
    }
}
