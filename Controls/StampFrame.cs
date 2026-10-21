using FlagsRally.Models;

namespace FlagsRally.Controls;

/// <summary>
/// The outline of a passport stamp, drawn to fill its bounds in the shape and ink of the pattern;
/// the stamp's text sits on top of it. The details follow real border stamps: the symbol of the
/// way the traveller came, as on Schengen stamps (a plane, car, ship, train or someone on foot,
/// each stamp its own), a ring of stars round a roundel, a sticker label with a code block (Japan's
/// landing permission), a port number, and the uneven ink of a rubber stamp.
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
            var travel = TravelFor(frame.Seed);

            canvas.StrokeColor = ink;
            canvas.FillColor = ink.WithAlpha(0.05f);
            canvas.StrokeLineCap = LineCap.Round;

            var outerRect = bounds.Inflate(-outer, -outer);
            var innerRect = outerRect.Inflate(-gap, -gap);

            switch (frame.Pattern)
            {
                case StampPattern.Circle:
                    DrawRoundel(canvas, outerRect, ink, outer, inner, small, travel);
                    break;
                case StampPattern.Oval:
                    canvas.FillEllipse(outerRect);
                    Stroke(canvas, outer, () => canvas.DrawEllipse(outerRect));
                    Stroke(canvas, inner, () => canvas.DrawEllipse(innerRect));
                    if (!small)
                    {
                        // The way the traveller came at the left, and where it ended at the right: on foot
                        Travel(canvas, ink, travel, new PointF(innerRect.Left + 13, innerRect.Center.Y), 18);
                        Travel(canvas, ink, travel == TravelMode.Walk ? TravelMode.Plane : TravelMode.Walk,
                            new PointF(innerRect.Right - 13, innerRect.Center.Y), 18);
                    }
                    break;
                case StampPattern.Octagon:
                    canvas.FillPath(Octagon(outerRect));
                    Stroke(canvas, outer, () => canvas.DrawPath(Octagon(outerRect)));
                    Dashed(canvas, inner, () => canvas.DrawPath(Octagon(innerRect)));
                    if (!small)
                    {
                        // Rivets at the corners, the way the traveller came at the top right and the port number at the bottom
                        canvas.FillColor = ink;
                        foreach (var point in Corners(Octagon(innerRect.Inflate(-6, -6))))
                        {
                            canvas.FillCircle(point, 1.8f);
                        }
                        var cut = Math.Min(innerRect.Width, innerRect.Height) * 0.24f;
                        Travel(canvas, ink, travel, new PointF(innerRect.Right - cut * 0.55f - 4, innerRect.Top + cut * 0.55f + 4), 18);
                        canvas.FontColor = ink;
                        canvas.FontSize = 9;
                        canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
                        canvas.DrawString($"No. {random.Next(10, 99)}", innerRect.Left, innerRect.Bottom - 15,
                            innerRect.Width, 12, HorizontalAlignment.Center, VerticalAlignment.Center);
                    }
                    break;
                case StampPattern.Postage:
                    DrawSticker(canvas, outerRect, ink, inner, outer, gap, small, random, travel);
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

        // A roundel: a thick ring and a thin one, with every way of travelling between them, stars
        // in between, and the traveller's own way at the top. A true circle unless the text (large
        // text sizes) makes the stamp taller than wide; it then stretches upright so the text stays inside.
        static void DrawRoundel(ICanvas canvas, RectF rect, Color ink, float outer, float inner, bool small, TravelMode travel)
        {
            var size = Math.Min(rect.Width, rect.Height);
            var circle = rect.Height > rect.Width * 1.05f
                ? rect
                : new RectF(rect.Center.X - size / 2, rect.Center.Y - size / 2, size, size);
            var band = small ? 3f : 16f;
            var innerCircle = circle.Inflate(-band, -band);

            canvas.FillEllipse(circle);
            Stroke(canvas, outer, () => canvas.DrawEllipse(circle));
            Stroke(canvas, inner, () => canvas.DrawEllipse(innerCircle));
            if (small) return;

            var centre = circle.Center;
            var radiusX = circle.Width / 2 - band / 2;
            var radiusY = circle.Height / 2 - band / 2;
            // Five ways round the ring, each followed by two stars
            const int count = 15;
            var modes = Enum.GetValues<TravelMode>();
            for (var i = 0; i < count; i++)
            {
                var angle = -MathF.PI / 2 + i * 2 * MathF.PI / count;
                var point = new PointF(centre.X + radiusX * MathF.Cos(angle), centre.Y + radiusY * MathF.Sin(angle));
                if (i % 3 == 0)
                {
                    var mode = modes[((int)travel + i / 3) % modes.Length];
                    Travel(canvas, ink, mode, point, 13.5f);
                }
                else
                {
                    canvas.FillColor = ink;
                    canvas.FillPath(Star(point, 3.2f));
                }
            }
        }

        // A sticker like Japan's landing permission: a perforated paper label lying on the page,
        // with the way the traveller came at the top left and a block of code at the top right.
        static void DrawSticker(ICanvas canvas, RectF rect, Color ink, float inner, float outer, float gap, bool small, Random random, TravelMode travel)
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

            Travel(canvas, ink, travel, new PointF(panel.Left + 13, panel.Top + 12), 17);

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

        enum TravelMode { Plane, Car, Ship, Train, Walk }

        // Each stamp its own way of arriving, kept every time the passport opens.
        static TravelMode TravelFor(int seed) => (TravelMode)((unchecked((uint)seed * 2654435761u) >> 16) % 5);

        // The symbol of a way of travelling, about size points across, centred on the point.
        static void Travel(ICanvas canvas, Color ink, TravelMode mode, PointF centre, float size)
        {
            switch (mode)
            {
                case TravelMode.Plane: Plane(canvas, ink, centre, size, 45); break;
                case TravelMode.Car: Car(canvas, ink, centre, size); break;
                case TravelMode.Ship: Ship(canvas, ink, centre, size); break;
                case TravelMode.Train: Train(canvas, ink, centre, size); break;
                default: Walker(canvas, ink, centre, size); break;
            }
        }

        // A car from the side: body, cabin and two wheels with paper-coloured hubs.
        static void Car(ICanvas canvas, Color ink, PointF c, float size)
        {
            var s = size / 24f;
            canvas.FillColor = ink;
            canvas.FillRoundedRectangle(c.X - 11 * s, c.Y - 2 * s, 22 * s, 7 * s, 2 * s);
            var cabin = new PathF();
            cabin.MoveTo(c.X - 6 * s, c.Y - 2 * s);
            cabin.LineTo(c.X - 3.5f * s, c.Y - 7.5f * s);
            cabin.LineTo(c.X + 4 * s, c.Y - 7.5f * s);
            cabin.LineTo(c.X + 7 * s, c.Y - 2 * s);
            cabin.Close();
            canvas.FillPath(cabin);
            canvas.FillColor = Paper;
            canvas.FillRectangle(c.X - 2.5f * s, c.Y - 6 * s, 2.2f * s, 3.2f * s);
            canvas.FillRectangle(c.X + 0.8f * s, c.Y - 6 * s, 2.6f * s, 3.2f * s);
            foreach (var x in new[] { c.X - 6 * s, c.X + 6 * s })
            {
                canvas.FillColor = ink;
                canvas.FillCircle(x, c.Y + 5.5f * s, 3.2f * s);
                canvas.FillColor = Paper;
                canvas.FillCircle(x, c.Y + 5.5f * s, 1.2f * s);
            }
        }

        // A ship from the side: hull, deck house, funnel and a wave below.
        static void Ship(ICanvas canvas, Color ink, PointF c, float size)
        {
            var s = size / 24f;
            canvas.FillColor = ink;
            var hull = new PathF();
            hull.MoveTo(c.X - 12 * s, c.Y + 1 * s);
            hull.LineTo(c.X + 12 * s, c.Y + 1 * s);
            hull.LineTo(c.X + 8 * s, c.Y + 7 * s);
            hull.LineTo(c.X - 9 * s, c.Y + 7 * s);
            hull.Close();
            canvas.FillPath(hull);
            canvas.FillRectangle(c.X - 6 * s, c.Y - 4 * s, 11 * s, 5 * s);
            canvas.FillRectangle(c.X - 1 * s, c.Y - 9 * s, 3.5f * s, 5 * s);
            canvas.StrokeColor = ink;
            canvas.StrokeSize = 1.2f * s + 0.4f;
            canvas.StrokeDashPattern = null;
            var wave = new PathF();
            wave.MoveTo(c.X - 12 * s, c.Y + 10 * s);
            wave.QuadTo(c.X - 9 * s, c.Y + 8 * s, c.X - 6 * s, c.Y + 10 * s);
            wave.QuadTo(c.X - 3 * s, c.Y + 12 * s, c.X, c.Y + 10 * s);
            wave.QuadTo(c.X + 3 * s, c.Y + 8 * s, c.X + 6 * s, c.Y + 10 * s);
            wave.QuadTo(c.X + 9 * s, c.Y + 12 * s, c.X + 12 * s, c.Y + 10 * s);
            canvas.DrawPath(wave);
        }

        // A train from the front: rounded body, window, two lamps and the rails.
        static void Train(ICanvas canvas, Color ink, PointF c, float size)
        {
            var s = size / 24f;
            canvas.FillColor = ink;
            canvas.FillRoundedRectangle(c.X - 7 * s, c.Y - 11 * s, 14 * s, 17 * s, 4 * s);
            canvas.FillColor = Paper;
            canvas.FillRoundedRectangle(c.X - 5 * s, c.Y - 8.5f * s, 10 * s, 6 * s, 1.5f * s);
            canvas.FillCircle(c.X - 3.5f * s, c.Y + 2 * s, 1.4f * s);
            canvas.FillCircle(c.X + 3.5f * s, c.Y + 2 * s, 1.4f * s);
            canvas.StrokeColor = ink;
            canvas.StrokeSize = 1.4f * s + 0.4f;
            canvas.StrokeDashPattern = null;
            canvas.DrawLine(c.X - 5 * s, c.Y + 6 * s, c.X - 8 * s, c.Y + 11 * s);
            canvas.DrawLine(c.X + 5 * s, c.Y + 6 * s, c.X + 8 * s, c.Y + 11 * s);
            canvas.DrawLine(c.X - 9 * s, c.Y + 11 * s, c.X + 9 * s, c.Y + 11 * s);
        }

        // Someone on foot, mid-stride, as on a road sign.
        static void Walker(ICanvas canvas, Color ink, PointF c, float size)
        {
            var s = size / 24f;
            canvas.FillColor = ink;
            canvas.FillCircle(c.X + 1 * s, c.Y - 9.5f * s, 2.6f * s);
            canvas.StrokeColor = ink;
            canvas.StrokeSize = 2.6f * s + 0.3f;
            canvas.StrokeDashPattern = null;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeLineJoin = LineJoin.Round;
            canvas.DrawLine(c.X + 0.5f * s, c.Y - 5.5f * s, c.X - 1 * s, c.Y + 2.5f * s);             // body
            var legs = new PathF();
            legs.MoveTo(c.X - 6 * s, c.Y + 11 * s);
            legs.LineTo(c.X - 3.5f * s, c.Y + 6 * s);
            legs.LineTo(c.X - 1 * s, c.Y + 2.5f * s);
            legs.LineTo(c.X + 2.5f * s, c.Y + 6.5f * s);
            legs.LineTo(c.X + 5 * s, c.Y + 11 * s);
            canvas.DrawPath(legs);
            var arms = new PathF();
            arms.MoveTo(c.X - 5 * s, c.Y + 0.5f * s);
            arms.LineTo(c.X - 2 * s, c.Y - 4 * s);
            arms.LineTo(c.X + 0.5f * s, c.Y - 4.5f * s);
            arms.LineTo(c.X + 3.5f * s, c.Y - 1 * s);
            arms.LineTo(c.X + 6 * s, c.Y - 0.5f * s);
            canvas.DrawPath(arms);
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
