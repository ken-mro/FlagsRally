using FlagsRally.Models;

namespace FlagsRally.Controls;

/// <summary>
/// The outline of a passport stamp, drawn to fill its bounds in the shape and ink of the pattern;
/// the stamp's text sits on top of it. The details follow real border stamps: the plane of the
/// means of travel (Schengen stamps), a ring of stars round an airport roundel, a sticker label
/// with a code block (Japan's landing permission), a port number, and the uneven ink of a rubber stamp.
/// One view per stamp, so a long passport stays as quick to scroll as with plain borders.
/// </summary>
public class StampFrame : GraphicsView
{
    public static readonly BindableProperty PatternProperty = BindableProperty.Create(
        nameof(Pattern), typeof(StampPattern), typeof(StampFrame), StampPattern.Square,
        propertyChanged: (view, _, _) => ((StampFrame)view).Invalidate());

    /// <summary>Thinner lines and no details, for the small previews in Settings.</summary>
    public static readonly BindableProperty IsSmallProperty = BindableProperty.Create(
        nameof(IsSmall), typeof(bool), typeof(StampFrame), false,
        propertyChanged: (view, _, _) => ((StampFrame)view).Invalidate());

    /// <summary>Picks where the ink is thin and the port number, so each stamp keeps its own.</summary>
    public static readonly BindableProperty SeedProperty = BindableProperty.Create(
        nameof(Seed), typeof(int), typeof(StampFrame), 0,
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

    public int Seed
    {
        get => (int)GetValue(SeedProperty);
        set => SetValue(SeedProperty, value);
    }

    sealed class FrameDrawable(StampFrame frame) : IDrawable
    {
        static readonly Color Paper = Color.FromArgb("#F4EEDF");

        public void Draw(ICanvas canvas, RectF bounds)
        {
            var small = frame.IsSmall;
            var outer = small ? 1.6f : 3.5f;
            var inner = small ? 0.8f : 1.5f;
            var gap = small ? 2.5f : 5f;
            var ink = StampPatterns.InkFor(frame.Pattern);
            var random = new Random(frame.Seed * 31 + (int)frame.Pattern);

            canvas.StrokeColor = ink;
            canvas.FillColor = ink.WithAlpha(0.05f);
            canvas.StrokeLineCap = LineCap.Round;

            var outerRect = bounds.Inflate(-outer, -outer);
            var innerRect = outerRect.Inflate(-gap, -gap);

            switch (frame.Pattern)
            {
                case StampPattern.Circle:
                    DrawRoundel(canvas, outerRect, ink, outer, inner, small);
                    break;
                case StampPattern.Oval:
                    canvas.FillEllipse(outerRect);
                    Stroke(canvas, outer, () => canvas.DrawEllipse(outerRect));
                    Stroke(canvas, inner, () => canvas.DrawEllipse(innerRect));
                    if (!small)
                    {
                        // A plane at each end, both flying the same way
                        Plane(canvas, ink, new PointF(innerRect.Left + 11, innerRect.Center.Y), 13, 90);
                        Plane(canvas, ink, new PointF(innerRect.Right - 11, innerRect.Center.Y), 13, 90);
                    }
                    break;
                case StampPattern.Octagon:
                    canvas.FillPath(Octagon(outerRect));
                    Stroke(canvas, outer, () => canvas.DrawPath(Octagon(outerRect)));
                    Dashed(canvas, inner, () => canvas.DrawPath(Octagon(innerRect)));
                    if (!small)
                    {
                        // Rivets at the corners, a plane at the top right and the port number at the bottom
                        canvas.FillColor = ink;
                        foreach (var point in Corners(Octagon(innerRect.Inflate(-6, -6))))
                        {
                            canvas.FillCircle(point, 1.8f);
                        }
                        var cut = Math.Min(innerRect.Width, innerRect.Height) * 0.24f;
                        Plane(canvas, ink, new PointF(innerRect.Right - cut * 0.55f - 4, innerRect.Top + cut * 0.55f + 4), 13, 45);
                        canvas.FontColor = ink;
                        canvas.FontSize = 9;
                        canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
                        canvas.DrawString($"No. {random.Next(10, 99)}", innerRect.Left, innerRect.Bottom - 15,
                            innerRect.Width, 12, HorizontalAlignment.Center, VerticalAlignment.Center);
                    }
                    break;
                case StampPattern.Postage:
                    DrawSticker(canvas, outerRect, ink, inner, outer, gap, small, random);
                    break;
                default:
                    Stroke(canvas, outer, () => canvas.DrawRectangle(outerRect));
                    Dashed(canvas, inner, () => canvas.DrawRectangle(innerRect));
                    break;
            }

            if (!small && frame.Pattern != StampPattern.Postage)
            {
                InkGaps(canvas, bounds, random);
            }
        }

        // An airport roundel: a thick ring and a thin one, with a ring of stars between them and a
        // plane at the top in place of one star. A true circle unless the text (large text sizes)
        // makes the stamp taller than wide; it then stretches upright so the text stays inside.
        static void DrawRoundel(ICanvas canvas, RectF rect, Color ink, float outer, float inner, bool small)
        {
            var size = Math.Min(rect.Width, rect.Height);
            var circle = rect.Height > rect.Width * 1.05f
                ? rect
                : new RectF(rect.Center.X - size / 2, rect.Center.Y - size / 2, size, size);
            var band = small ? 3f : 13f;
            var innerCircle = circle.Inflate(-band, -band);

            canvas.FillEllipse(circle);
            Stroke(canvas, outer, () => canvas.DrawEllipse(circle));
            Stroke(canvas, inner, () => canvas.DrawEllipse(innerCircle));
            if (small) return;

            var centre = circle.Center;
            var radiusX = circle.Width / 2 - band / 2;
            var radiusY = circle.Height / 2 - band / 2;
            const int count = 16;
            canvas.FillColor = ink;
            for (var i = 0; i < count; i++)
            {
                var angle = -MathF.PI / 2 + i * 2 * MathF.PI / count;
                var point = new PointF(centre.X + radiusX * MathF.Cos(angle), centre.Y + radiusY * MathF.Sin(angle));
                if (i == 0)
                {
                    Plane(canvas, ink, point, 11, 90);
                }
                else
                {
                    canvas.FillPath(Star(point, 3.2f));
                }
            }
        }

        // A sticker like Japan's landing permission: a perforated paper label lying on the page,
        // with a plane at the top left and a block of code at the top right.
        static void DrawSticker(ICanvas canvas, RectF rect, Color ink, float inner, float outer, float gap, bool small, Random random)
        {
            var edge = Perforated(rect, small ? 3f : 6f);
            if (!small)
            {
                canvas.SetShadow(new SizeF(0, 1.5f), 3, Colors.Black.WithAlpha(0.18f));
            }
            canvas.FillColor = Color.FromArgb("#FFFDF7");
            canvas.FillPath(edge);
            canvas.SetShadow(SizeF.Zero, 0, null);
            canvas.FillColor = ink.WithAlpha(0.06f);
            canvas.FillPath(edge);
            Stroke(canvas, inner, () => canvas.DrawPath(edge));
            var panel = rect.Inflate(-gap * 1.4f, -gap * 1.4f);
            Stroke(canvas, outer * 0.6f, () => canvas.DrawRectangle(panel));
            if (small) return;

            Plane(canvas, ink, new PointF(panel.Left + 11, panel.Top + 11), 13, 45);

            // A small square of dots, like the sticker's code
            const int cells = 5;
            const float cell = 2.6f;
            var left = panel.Right - 6 - cells * cell;
            var top = panel.Top + 5;
            canvas.FillColor = ink;
            for (var row = 0; row < cells; row++)
            {
                for (var column = 0; column < cells; column++)
                {
                    var corner = (row < 2 && column < 2) || (row < 2 && column > cells - 3) || (row > cells - 3 && column < 2);
                    if (corner || random.Next(2) == 0)
                    {
                        canvas.FillRectangle(left + column * cell, top + row * cell, cell, cell);
                    }
                }
            }
        }

        // Where a rubber stamp did not take the ink: small paper-coloured specks over the lines.
        static void InkGaps(ICanvas canvas, RectF bounds, Random random)
        {
            canvas.FillColor = Paper.WithAlpha(0.85f);
            var count = (int)(bounds.Width * bounds.Height / 220);
            for (var i = 0; i < count; i++)
            {
                var x = bounds.Left + (float)random.NextDouble() * bounds.Width;
                var y = bounds.Top + (float)random.NextDouble() * bounds.Height;
                canvas.FillCircle(x, y, 0.5f + (float)random.NextDouble() * 1.1f);
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

        // A plane seen from above, nose up before turning by the given degrees, in the ink.
        static void Plane(ICanvas canvas, Color ink, PointF centre, float size, float degrees)
        {
            canvas.SaveState();
            canvas.Translate(centre.X, centre.Y);
            canvas.Rotate(degrees);
            var s = size / 24f;
            var path = new PathF();
            path.MoveTo(0, -12 * s);
            path.CurveTo(1.6f * s, -12 * s, 1.8f * s, -10 * s, 1.8f * s, -8 * s);
            path.LineTo(1.8f * s, -2.5f * s);
            path.LineTo(11 * s, 3 * s);
            path.LineTo(11 * s, 5.5f * s);
            path.LineTo(1.8f * s, 2.5f * s);
            path.LineTo(1.8f * s, 8 * s);
            path.LineTo(4.5f * s, 10.5f * s);
            path.LineTo(4.5f * s, 12 * s);
            path.LineTo(0, 10.8f * s);
            path.LineTo(-4.5f * s, 12 * s);
            path.LineTo(-4.5f * s, 10.5f * s);
            path.LineTo(-1.8f * s, 8 * s);
            path.LineTo(-1.8f * s, 2.5f * s);
            path.LineTo(-11 * s, 5.5f * s);
            path.LineTo(-11 * s, 3 * s);
            path.LineTo(-1.8f * s, -2.5f * s);
            path.LineTo(-1.8f * s, -8 * s);
            path.CurveTo(-1.8f * s, -10 * s, -1.6f * s, -12 * s, 0, -12 * s);
            path.Close();
            canvas.FillColor = ink;
            canvas.FillPath(path);
            canvas.RestoreState();
        }

        static PathF Star(PointF centre, float radius)
        {
            var path = new PathF();
            for (var i = 0; i < 10; i++)
            {
                var r = i % 2 == 0 ? radius : radius * 0.45f;
                var angle = -MathF.PI / 2 + i * MathF.PI / 5;
                var point = new PointF(centre.X + r * MathF.Cos(angle), centre.Y + r * MathF.Sin(angle));
                if (i == 0) path.MoveTo(point); else path.LineTo(point);
            }
            path.Close();
            return path;
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

        static IEnumerable<PointF> Corners(PathF path)
        {
            for (var i = 0; i < path.Count; i++)
            {
                yield return path[i];
            }
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
