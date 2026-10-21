using FlagsRally.Models;

namespace FlagsRally.Controls;

/// <summary>
/// The outline and motifs of a passport stamp, drawn to fill its bounds in the pattern's ink; the
/// stamp's text sits on top, inside the pattern's padding (see <see cref="StampPatterns.PaddingFor"/>).
/// The motifs follow travel stamps and postmarks: globes, compass roses, the wavy lines of a
/// postmark, an airmail edge, world maps, and the uneven ink of a rubber stamp. Travel appears only
/// as the whole row (plane, ship, train, car, on foot), never one means on its own: the app does not
/// know how the traveller came.
/// One view per stamp, so a long passport stays as quick to scroll as with plain borders.
/// </summary>
public class StampFrame : GraphicsView
{
    public static readonly BindableProperty PatternProperty = BindableProperty.Create(
        nameof(Pattern), typeof(StampPattern), typeof(StampFrame), StampPattern.Square,
        propertyChanged: (view, _, _) => ((StampFrame)view).Invalidate());

    /// <summary>Thinner lines and fewer motifs, for the small previews in Settings.</summary>
    public static readonly BindableProperty IsSmallProperty = BindableProperty.Create(
        nameof(IsSmall), typeof(bool), typeof(StampFrame), false,
        propertyChanged: (view, _, _) => ((StampFrame)view).Invalidate());

    /// <summary>Picks where the ink is thin, so each stamp keeps its own.</summary>
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
            var d = new Pen(canvas, StampPatterns.InkFor(frame.Pattern), frame.IsSmall);
            var r = bounds.Inflate(-d.Outer, -d.Outer);
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeLineJoin = LineJoin.Round;

            switch (frame.Pattern)
            {
                case StampPattern.Airmail: Airmail(d, r); break;
                case StampPattern.CompassHexagon: CompassHexagon(d, r); break;
                case StampPattern.Sunrise: Sunrise(d, r); break;
                case StampPattern.ScallopedGlobe: ScallopedGlobe(d, r); break;
                case StampPattern.WorldOval: WorldOval(d, r); break;
                case StampPattern.Ticket: Ticket(d, r); break;
                case StampPattern.CompassSeal: CompassSeal(d, r); break;
                case StampPattern.Postcard: Postcard(d, r); break;
                case StampPattern.Triangle: Triangle(d, r); break;
                case StampPattern.StarSeal: StarSeal(d, r); break;
                default:
                    d.Stroke(d.Outer, () => canvas.DrawRectangle(r));
                    d.Dashed(d.Inner, () => canvas.DrawRectangle(r.Inflate(-d.Gap, -d.Gap)));
                    break;
            }

            if (!frame.IsSmall && frame.Pattern != StampPattern.Square)
            {
                InkGaps(canvas, bounds, new Random(frame.Seed * 31 + (int)frame.Pattern));
            }
        }

        // ---------- the designs ----------

        // An airmail envelope: the striped edge, postmark waves at the top right and a globe below.
        static void Airmail(Pen d, RectF r)
        {
            d.FillTint(() => d.Canvas.FillRoundedRectangle(r, 8));
            d.Stroke(d.Inner, () => d.Canvas.DrawRoundedRectangle(r, 8));
            var stripe = d.Small ? 2.5f : 6f;
            AirmailStripes(d, r.Inflate(-d.Inner, -d.Inner), stripe);
            var inner = r.Inflate(-stripe - d.Inner * 2, -stripe - d.Inner * 2);
            d.Stroke(d.Inner, () => d.Canvas.DrawRectangle(inner));
            if (d.Small) return;
            Waves(d, new RectF(inner.Right - 48, inner.Top + 4, 42, 12), 3);
            Globe(d, new PointF(inner.Left + 12, inner.Top + 12), 7);
        }

        // A hexagon on its point: a compass rose in a ring with N, W and E at the top, a dashed ring
        // round the text, sparkles at the sides, a row of ways to travel and a sparkle at the bottom.
        static void CompassHexagon(Pen d, RectF r)
        {
            var outer = Hexagon(r);
            d.FillTint(() => d.Canvas.FillPath(outer));
            d.Stroke(d.Outer, () => d.Canvas.DrawPath(outer));
            d.Stroke(d.Inner, () => d.Canvas.DrawPath(Hexagon(r.Inflate(-d.Gap, -d.Gap))));
            if (d.Small) return;
            var c = r.Center;
            var rose = new PointF(c.X, r.Top + 30);
            // Dashed arcs either side of the text, like a ring round it
            var around = new RectF(r.Left + 22, r.Top + 30, r.Width - 44, r.Height - 64);
            d.Dashed(d.Inner, () =>
            {
                d.Canvas.DrawPath(ArcPath(around, 140, 220));
                d.Canvas.DrawPath(ArcPath(around, -40, 40));
            });
            CompassRose(d, rose, 12, letters: true, ring: true);
            Sparkle(d, new PointF(r.Left + 14, c.Y), 4.5f);
            Sparkle(d, new PointF(r.Right - 14, c.Y), 4.5f);
            TravelRow(d, new PointF(c.X, r.Bottom - 30), 13, 21, TravelMode.Plane, TravelMode.Ship, TravelMode.Car, TravelMode.Walk);
            Sparkle(d, new PointF(c.X, r.Bottom - 13), 3.5f);
        }

        // A double ring with a sparkle at the top and a fan of rays below it, a dotted ring, a sparkle
        // at the left and the bottom, and postmark waves running off the right side.
        static void Sunrise(Pen d, RectF r)
        {
            var ring = Round(r);
            d.FillTint(() => d.Canvas.FillEllipse(ring));
            d.Stroke(d.Outer, () => d.Canvas.DrawEllipse(ring));
            d.Stroke(d.Inner, () => d.Canvas.DrawEllipse(ring.Inflate(-d.Gap, -d.Gap)));
            if (d.Small) return;
            var c = ring.Center;
            var inner = ring.Inflate(-d.Gap - 6, -d.Gap - 6);
            Dotted(d, inner, 44, 0.9f);
            // Rays fanning out round the top, as if from the middle of the stamp
            d.Stroke(d.Inner * 0.8f, () =>
            {
                for (var i = 0; i <= 12; i++)
                {
                    var angle = -MathF.PI / 2 + (i - 6) * 0.13f;
                    var (cos, sin) = (MathF.Cos(angle), MathF.Sin(angle));
                    var from = inner.Height / 2 - (i % 2 == 0 ? 20 : 15);
                    var to = inner.Height / 2 - 6;
                    d.Canvas.DrawLine(c.X + from * cos, c.Y + from * sin, c.X + to * cos, c.Y + to * sin);
                }
            });
            Sparkle(d, new PointF(c.X, ring.Top + 15), 6);
            Sparkle(d, new PointF(ring.Left + 17, c.Y), 4.5f);
            Sparkle(d, new PointF(c.X, ring.Bottom - 15), 6);
            d.Stroke(d.Inner, () =>
            {
                d.Canvas.DrawLine(c.X - 22, ring.Bottom - 15, c.X - 12, ring.Bottom - 15);
                d.Canvas.DrawLine(c.X + 12, ring.Bottom - 15, c.X + 22, ring.Bottom - 15);
            });
            // The postmark's waves start inside the ring and run past its edge
            var right = Math.Min(r.Right + d.Outer, ring.Right + 22);
            Waves(d, new RectF(ring.Right - 26, c.Y - 18, right - (ring.Right - 26), 30), 5);
        }

        // A bumpy edge and a ring round a large globe at the top, sparkles at the sides, a row of
        // ways to travel and a small diamond at the bottom.
        static void ScallopedGlobe(Pen d, RectF r)
        {
            var edge = Scalloped(r, d.Small ? 12 : 18, d.Small ? 1.5f : 4.5f);
            d.FillTint(() => d.Canvas.FillPath(edge));
            d.Stroke(d.Outer, () => d.Canvas.DrawPath(edge));
            var ring = r.Inflate(-d.Gap * 2, -d.Gap * 2);
            d.Stroke(d.Inner, () => d.Canvas.DrawEllipse(ring));
            if (d.Small) return;
            var c = ring.Center;
            // The top of a big globe, cut off by the text below it
            var globe = new PointF(c.X, ring.Top + 30);
            d.Canvas.SaveState();
            d.Canvas.ClipRectangle(ring.Left, ring.Top, ring.Width, 34);
            Globe(d, globe, 24, lines: 4);
            d.Canvas.RestoreState();
            Sparkle(d, new PointF(ring.Left + 13, c.Y), 4.5f);
            Sparkle(d, new PointF(ring.Right - 13, c.Y), 4.5f);
            TravelRow(d, new PointF(c.X, ring.Bottom - 20), 12, 19, TravelMode.Plane, TravelMode.Ship, TravelMode.Car, TravelMode.Walk);
            d.Canvas.FillColor = d.Ink;
            d.Canvas.FillPath(Diamond(new PointF(c.X, ring.Bottom - 7), 2.5f));
        }

        // An oval with the world's lands in dots at the sides, postmark waves and a dotted route.
        static void WorldOval(Pen d, RectF r)
        {
            d.FillTint(() => d.Canvas.FillEllipse(r));
            d.Stroke(d.Outer, () => d.Canvas.DrawEllipse(r));
            var inner = r.Inflate(-d.Gap, -d.Gap);
            d.Stroke(d.Inner, () => d.Canvas.DrawEllipse(inner));
            if (d.Small) return;
            WorldMap(d, new RectF(inner.Left + 4, inner.Top + inner.Height * 0.14f, inner.Width - 8, inner.Height * 0.5f), clearCentre: 0.6f);
            Waves(d, new RectF(inner.Left + 12, inner.Center.Y + 12, 22, 12), 3);
            Waves(d, new RectF(inner.Right - 34, inner.Center.Y + 12, 22, 12), 3);
            Sparkle(d, new PointF(inner.Center.X, inner.Bottom - 6), 3);
        }

        // A ticket: notched corners and sides, speed lines along the edges, stars round a globe at the
        // top, the date between dashes, and below a line the row of ways to travel.
        static void Ticket(Pen d, RectF r)
        {
            var edge = TicketEdge(r, d.Small ? 3 : 9, d.Small ? 2.5f : 6);
            d.FillTint(() => d.Canvas.FillPath(edge));
            d.Stroke(d.Outer, () => d.Canvas.DrawPath(edge));
            var inner = r.Inflate(-d.Gap * 1.6f, -d.Gap * 1.6f);
            d.Stroke(d.Inner, () => d.Canvas.DrawPath(TicketEdge(inner, 6, 0)));
            if (d.Small) return;
            Globe(d, new PointF(inner.Center.X, inner.Top + 13), 9);
            foreach (var x in new[] { -40f, -27f, 27f, 40f })
            {
                d.Canvas.FillColor = d.Ink;
                d.Canvas.FillPath(Star(new PointF(inner.Center.X + x, inner.Top + 13), 4.5f));
            }
            // Speed lines on both edges, above and below the notches
            d.Stroke(d.Inner * 0.8f, () =>
            {
                foreach (var y in new[] { inner.Top + 30, inner.Top + 34, inner.Top + 38, inner.Bottom - 46, inner.Bottom - 42, inner.Bottom - 38 })
                {
                    d.Canvas.DrawLine(inner.Left + 3, y, inner.Left + 11, y);
                    d.Canvas.DrawLine(inner.Right - 11, y, inner.Right - 3, y);
                }
            });
            d.Stroke(d.Inner, () => d.Canvas.DrawLine(inner.Left + 4, inner.Bottom - 26, inner.Right - 4, inner.Bottom - 26));
            TravelRow(d, new PointF(inner.Center.X, inner.Bottom - 13), 13, 20,
                TravelMode.Plane, TravelMode.Ship, TravelMode.Train, TravelMode.Car, TravelMode.Walk);
        }

        // A compass card: a double ring with ticks, a tall needle under the N, lines to the corners,
        // arrows out to W and E, and a star over the S.
        static void CompassSeal(Pen d, RectF r)
        {
            var ring = Round(r);
            d.FillTint(() => d.Canvas.FillEllipse(ring));
            d.Stroke(d.Outer, () => d.Canvas.DrawEllipse(ring));
            var inner = ring.Inflate(-d.Gap, -d.Gap);
            d.Stroke(d.Inner, () => d.Canvas.DrawEllipse(inner));
            if (d.Small) return;
            var c = ring.Center;
            var rx = inner.Width / 2;
            var ry = inner.Height / 2;
            d.Stroke(d.Inner * 0.8f, () =>
            {
                for (var i = 0; i < 48; i++)
                {
                    var angle = i * MathF.PI * 2 / 48;
                    var length = i % 6 == 0 ? 6f : 3f;
                    var (cos, sin) = (MathF.Cos(angle), MathF.Sin(angle));
                    d.Canvas.DrawLine(c.X + rx * cos, c.Y + ry * sin, c.X + (rx - length) * cos, c.Y + (ry - length) * sin);
                }
                // Lines out to the corners, between the ticks and the text
                foreach (var angle in new[] { -0.75f, -2.39f, 0.75f, 2.39f })
                {
                    var (cos, sin) = (MathF.Cos(angle), MathF.Sin(angle));
                    var from = angle < 0 ? 28 : 16; // the lower ones stay clear of the date
                    d.Canvas.DrawLine(c.X + (rx - from) * cos, c.Y + (ry - from) * sin, c.X + (rx - 9) * cos, c.Y + (ry - 9) * sin);
                }
            });
            Letter(d, "N", new PointF(c.X, inner.Top + 11), 12);
            Letter(d, "W", new PointF(inner.Left + 11, c.Y), 12);
            Letter(d, "E", new PointF(inner.Right - 11, c.Y), 12);
            Letter(d, "S", new PointF(c.X, inner.Bottom - 10), 12);
            // The needle, half solid like a printed compass
            var tip = new PointF(c.X, inner.Top + 19);
            var foot = inner.Top + 36;
            var solid = new PathF();
            solid.MoveTo(tip);
            solid.LineTo(c.X + 4, foot);
            solid.LineTo(c.X, foot - 2);
            solid.Close();
            d.Canvas.FillColor = d.Ink;
            d.Canvas.FillPath(solid);
            var open = new PathF();
            open.MoveTo(tip);
            open.LineTo(c.X - 4, foot);
            open.LineTo(c.X, foot - 2);
            open.Close();
            d.Stroke(d.Inner * 0.8f, () => d.Canvas.DrawPath(open));
            // Arrowheads pointing out to W and E
            d.Canvas.FillColor = d.Ink;
            d.Canvas.FillPath(Arrow(new PointF(inner.Left + 21, c.Y), -1));
            d.Canvas.FillPath(Arrow(new PointF(inner.Right - 21, c.Y), 1));
            // A star over the S
            d.Canvas.FillPath(StarPoints(new PointF(c.X, inner.Bottom - 24), 6, 4));
        }

        // A postcard: a frame, postmark waves at the top right and bottom left, and address lines.
        static void Postcard(Pen d, RectF r)
        {
            d.FillTint(() => d.Canvas.FillRoundedRectangle(r, 10));
            d.Stroke(d.Outer, () => d.Canvas.DrawRoundedRectangle(r, 10));
            var inner = r.Inflate(-d.Gap, -d.Gap);
            d.Stroke(d.Inner, () => d.Canvas.DrawRoundedRectangle(inner, 7));
            if (d.Small) return;
            Waves(d, new RectF(inner.Right - 6 - inner.Width * 0.3f, inner.Top + 4, inner.Width * 0.3f, 10), 3);
            Waves(d, new RectF(inner.Left + 6, inner.Bottom - 14, inner.Width * 0.3f, 10), 3);
            d.Stroke(d.Inner, () =>
            {
                for (var i = 0; i < 3; i++)
                {
                    var y = inner.Top + 5 + i * 4.5f;
                    d.Canvas.DrawLine(inner.Left + 8, y, inner.Left + 8 + inner.Width * 0.24f, y);
                    var y2 = inner.Bottom - 14 + i * 4.5f;
                    d.Canvas.DrawLine(inner.Right - 8 - inner.Width * 0.24f, y2, inner.Right - 8, y2);
                }
            });
        }

        // A triangle with a double line: a globe near the top with dashed routes curving down both
        // sides and sparkles along them. The text fills the wide lower half, so there is no travel row.
        static void Triangle(Pen d, RectF r)
        {
            var outer = TriangleEdge(r);
            d.FillTint(() => d.Canvas.FillPath(outer));
            d.Stroke(d.Outer, () => d.Canvas.DrawPath(outer));
            var inner = TriangleEdge(r.Inflate(-d.Gap * 1.6f, -d.Gap * 1.6f), top: d.Gap * 1.2f);
            d.Stroke(d.Inner, () => d.Canvas.DrawPath(inner));
            if (d.Small) return;
            var c = r.Center;
            var globe = new PointF(c.X, r.Top + 40);
            Globe(d, globe, 9);
            // Routes from the globe down the sides
            d.Dashed(d.Inner, () =>
            {
                var left = new PathF();
                left.MoveTo(globe.X - 13, globe.Y + 2);
                left.QuadTo(c.X - r.Width * 0.2f, r.Top + r.Height * 0.3f, c.X - r.Width * 0.24f, r.Top + r.Height * 0.52f);
                d.Canvas.DrawPath(left);
                var right = new PathF();
                right.MoveTo(globe.X + 13, globe.Y + 2);
                right.QuadTo(c.X + r.Width * 0.2f, r.Top + r.Height * 0.3f, c.X + r.Width * 0.24f, r.Top + r.Height * 0.52f);
                d.Canvas.DrawPath(right);
            });
            Sparkle(d, new PointF(c.X - 24, r.Top + 30), 3);
            Sparkle(d, new PointF(c.X - r.Width * 0.24f, r.Top + r.Height * 0.58f), 3.5f);
            Sparkle(d, new PointF(c.X + r.Width * 0.24f, r.Top + r.Height * 0.58f), 3.5f);
            Sparkle(d, new PointF(r.Left + 26, r.Bottom - 12), 3);
            Sparkle(d, new PointF(r.Right - 26, r.Bottom - 12), 3);
        }

        // A seal with a finely pinked edge, a solid ring and a dashed one, an eight-pointed star at the
        // top and sparkles at the sides and the bottom.
        static void StarSeal(Pen d, RectF r)
        {
            var edge = Scalloped(r, d.Small ? 20 : 34, d.Small ? 1.2f : 2.6f);
            d.FillTint(() => d.Canvas.FillPath(edge));
            d.Stroke(d.Outer * 0.9f, () => d.Canvas.DrawPath(edge));
            var ring = r.Inflate(-d.Gap * 1.6f, -d.Gap * 1.6f);
            d.Stroke(d.Inner, () => d.Canvas.DrawEllipse(ring));
            if (d.Small) return;
            d.Dashed(d.Inner, () => d.Canvas.DrawEllipse(ring.Inflate(-6, -6)));
            var c = ring.Center;
            CompassStar(d, new PointF(c.X, ring.Top + 21), 11);
            Sparkle(d, new PointF(ring.Left + 14, c.Y), 4.5f);
            Sparkle(d, new PointF(ring.Right - 14, c.Y), 4.5f);
            Sparkle(d, new PointF(c.X, ring.Bottom - 14), 6);
        }

        // ---------- motifs ----------

        // A globe: the outline, the equator and two parallels, and meridians.
        static void Globe(Pen d, PointF c, float radius, int lines = 2)
        {
            d.Stroke(d.Inner * 1.1f, () =>
            {
                d.Canvas.DrawCircle(c, radius);
                d.Canvas.DrawLine(c.X - radius, c.Y, c.X + radius, c.Y);
                foreach (var f in new[] { -0.5f, 0.5f })
                {
                    var y = c.Y + f * radius;
                    var half = radius * MathF.Sqrt(1 - f * f);
                    d.Canvas.DrawLine(c.X - half, y, c.X + half, y);
                }
                d.Canvas.DrawLine(c.X, c.Y - radius, c.X, c.Y + radius);
                for (var i = 1; i <= lines / 2; i++)
                {
                    var w = radius * 2 * i / (lines / 2 + 1);
                    d.Canvas.DrawEllipse(c.X - w / 2, c.Y - radius, w, radius * 2);
                }
            });
        }

        // An eight-pointed compass rose, with N, E and W round it when asked.
        static void CompassRose(Pen d, PointF c, float radius, bool letters, bool ring = false)
        {
            for (var i = 0; i < 8; i++)
            {
                var length = i % 2 == 0 ? radius : radius * 0.55f;
                var angle = -MathF.PI / 2 + i * MathF.PI / 4;
                var tip = new PointF(c.X + length * MathF.Cos(angle), c.Y + length * MathF.Sin(angle));
                var side = radius * 0.16f;
                var left = new PointF(c.X + side * MathF.Cos(angle - MathF.PI / 2), c.Y + side * MathF.Sin(angle - MathF.PI / 2));
                var right = new PointF(c.X + side * MathF.Cos(angle + MathF.PI / 2), c.Y + side * MathF.Sin(angle + MathF.PI / 2));
                // Each point half solid, half outline, as on a printed compass
                var solid = new PathF();
                solid.MoveTo(tip);
                solid.LineTo(left);
                solid.LineTo(c);
                solid.Close();
                d.Canvas.FillColor = d.Ink;
                d.Canvas.FillPath(solid);
                var outline = new PathF();
                outline.MoveTo(tip);
                outline.LineTo(right);
                outline.LineTo(c);
                outline.Close();
                d.Stroke(d.Inner * 0.7f, () => d.Canvas.DrawPath(outline));
            }
            d.Stroke(d.Inner * 0.8f, () => d.Canvas.DrawCircle(c, radius * 0.62f));
            if (ring)
            {
                d.Stroke(d.Inner * 0.8f, () =>
                {
                    d.Canvas.DrawCircle(c, radius * 0.88f);
                    d.Canvas.DrawCircle(c, radius * 0.98f);
                });
            }
            if (!letters) return;
            Letter(d, "N", new PointF(c.X, c.Y - radius - 6), 9);
            Letter(d, "W", new PointF(c.X - radius - 7, c.Y), 9);
            Letter(d, "E", new PointF(c.X + radius + 7, c.Y), 9);
        }

        // A four-pointed sparkle.
        static void Sparkle(Pen d, PointF c, float radius)
        {
            var path = new PathF();
            for (var i = 0; i < 8; i++)
            {
                var r = i % 2 == 0 ? radius : radius * 0.28f;
                var angle = -MathF.PI / 2 + i * MathF.PI / 4;
                var point = new PointF(c.X + r * MathF.Cos(angle), c.Y + r * MathF.Sin(angle));
                if (i == 0) path.MoveTo(point); else path.LineTo(point);
            }
            path.Close();
            d.Canvas.FillColor = d.Ink;
            d.Canvas.FillPath(path);
        }

        // The wavy lines a postmark cancels a stamp with.
        static void Waves(Pen d, RectF area, int count)
        {
            d.Stroke(d.Inner * 1.1f, () =>
            {
                for (var i = 0; i < count; i++)
                {
                    var y = area.Top + (count == 1 ? area.Height / 2 : i * area.Height / (count - 1));
                    var path = new PathF();
                    path.MoveTo(area.Left, y);
                    const float step = 7f;
                    var up = true;
                    for (var x = area.Left; x < area.Right - 0.1f; x += step)
                    {
                        var end = Math.Min(x + step, area.Right);
                        path.QuadTo((x + end) / 2, y + (up ? -2.2f : 2.2f), end, y);
                        up = !up;
                    }
                    d.Canvas.DrawPath(path);
                }
            });
        }

        enum TravelMode { Plane, Ship, Train, Car, Walk }

        // Several ways of travelling in a row, never one on its own: the app does not know which it was.
        static void TravelRow(Pen d, PointF centre, float size, float spacing, params TravelMode[] modes)
        {
            var start = centre.X - spacing * (modes.Length - 1) / 2f;
            for (var i = 0; i < modes.Length; i++)
            {
                var p = new PointF(start + i * spacing, centre.Y);
                switch (modes[i])
                {
                    case TravelMode.Plane: Plane(d, p, size); break;
                    case TravelMode.Ship: Ship(d, p, size); break;
                    case TravelMode.Train: Train(d, p, size); break;
                    case TravelMode.Car: Car(d, p, size); break;
                    default: Walker(d, p, size); break;
                }
            }
        }

        // Part of an ellipse, from one angle to another in degrees (0 at the right, going down).
        static PathF ArcPath(RectF ellipse, float from, float to)
        {
            var path = new PathF();
            var steps = 24;
            for (var i = 0; i <= steps; i++)
            {
                var a = (from + (to - from) * i / steps) * MathF.PI / 180;
                var point = new PointF(ellipse.Center.X + ellipse.Width / 2 * MathF.Cos(a), ellipse.Center.Y + ellipse.Height / 2 * MathF.Sin(a));
                if (i == 0) path.MoveTo(point); else path.LineTo(point);
            }
            return path;
        }

        // A ring of dots.
        static void Dotted(Pen d, RectF ellipse, int count, float radius)
        {
            d.Canvas.FillColor = d.Ink;
            for (var i = 0; i < count; i++)
            {
                var a = i * MathF.PI * 2 / count;
                d.Canvas.FillCircle(ellipse.Center.X + ellipse.Width / 2 * MathF.Cos(a), ellipse.Center.Y + ellipse.Height / 2 * MathF.Sin(a), radius);
            }
        }

        // An eight-pointed star of thin rays with a solid centre, as on a compass card.
        static void CompassStar(Pen d, PointF c, float radius)
        {
            d.Canvas.FillColor = d.Ink;
            d.Canvas.FillPath(StarPoints(c, radius, 8, inner: 0.22f));
            d.Stroke(d.Inner * 0.8f, () => d.Canvas.DrawCircle(c, radius * 0.45f));
        }

        // A star with the given number of points; inner is the waist as a share of the radius.
        static PathF StarPoints(PointF c, float radius, int points, float inner = 0.3f)
        {
            var path = new PathF();
            for (var i = 0; i < points * 2; i++)
            {
                var r = i % 2 == 0 ? radius : radius * inner;
                var angle = -MathF.PI / 2 + i * MathF.PI / points;
                var point = new PointF(c.X + r * MathF.Cos(angle), c.Y + r * MathF.Sin(angle));
                if (i == 0) path.MoveTo(point); else path.LineTo(point);
            }
            path.Close();
            return path;
        }

        static PathF Diamond(PointF c, float radius)
        {
            var path = new PathF();
            path.MoveTo(c.X, c.Y - radius * 1.4f);
            path.LineTo(c.X + radius, c.Y);
            path.LineTo(c.X, c.Y + radius * 1.4f);
            path.LineTo(c.X - radius, c.Y);
            path.Close();
            return path;
        }

        // A small arrowhead pointing left (-1) or right (1), its tip at the point.
        static PathF Arrow(PointF tip, int direction)
        {
            var path = new PathF();
            path.MoveTo(tip);
            path.LineTo(tip.X - direction * 7, tip.Y - 3.5f);
            path.LineTo(tip.X - direction * 5, tip.Y);
            path.LineTo(tip.X - direction * 7, tip.Y + 3.5f);
            path.Close();
            return path;
        }

        // The lands of the world as dots, from a few rough shapes; clearCentre leaves room for the text.
        static void WorldMap(Pen d, RectF area, float clearCentre)
        {
            // Rough continents, each a few overlapping ellipses: the Americas, Europe and Africa, Asia, Australia
            (float x, float y, float rx, float ry)[] lands =
            [
                (0.08f, 0.2f, 0.05f, 0.08f), (0.14f, 0.28f, 0.08f, 0.12f), (0.21f, 0.2f, 0.06f, 0.08f),
                (0.22f, 0.42f, 0.04f, 0.09f), (0.34f, 0.1f, 0.04f, 0.06f),
                (0.3f, 0.62f, 0.05f, 0.11f), (0.28f, 0.8f, 0.025f, 0.09f),
                (0.48f, 0.24f, 0.035f, 0.07f), (0.53f, 0.18f, 0.04f, 0.05f),
                (0.52f, 0.48f, 0.06f, 0.1f), (0.55f, 0.66f, 0.035f, 0.1f),
                (0.64f, 0.22f, 0.09f, 0.11f), (0.76f, 0.24f, 0.09f, 0.1f), (0.7f, 0.42f, 0.035f, 0.07f),
                (0.8f, 0.44f, 0.03f, 0.06f), (0.88f, 0.3f, 0.025f, 0.06f), (0.85f, 0.72f, 0.06f, 0.07f),
            ];
            d.Canvas.FillColor = d.Ink.WithAlpha(0.8f);
            const float step = 2.2f;
            for (var y = area.Top; y < area.Bottom; y += step)
            {
                for (var x = area.Left; x < area.Right; x += step)
                {
                    var u = (x - area.Left) / area.Width;
                    var v = (y - area.Top) / area.Height;
                    if (clearCentre > 0 && Math.Abs(u - 0.5f) < clearCentre / 2) continue;
                    foreach (var (lx, ly, rx, ry) in lands)
                    {
                        var dx = (u - lx) / rx;
                        var dy = (v - ly) / ry;
                        if (dx * dx + dy * dy <= 1)
                        {
                            d.Canvas.FillCircle(x, y, 0.8f);
                            break;
                        }
                    }
                }
            }
        }

        // An airmail envelope's edge: slanted stripes of ink and paper all the way round.
        static void AirmailStripes(Pen d, RectF r, float width)
        {
            d.Canvas.SaveState();
            var band = new PathF();
            band.AppendRectangle(r);
            band.AppendRectangle(r.Inflate(-width, -width), true);
            d.Canvas.ClipPath(band, WindingMode.EvenOdd);
            d.Canvas.FillColor = d.Ink;
            var stripe = width * 1.6f;
            for (var x = r.Left - r.Height; x < r.Right + r.Height; x += stripe * 2)
            {
                var path = new PathF();
                path.MoveTo(x, r.Bottom);
                path.LineTo(x + stripe, r.Bottom);
                path.LineTo(x + stripe + r.Height, r.Top);
                path.LineTo(x + r.Height, r.Top);
                path.Close();
                d.Canvas.FillPath(path);
            }
            d.Canvas.RestoreState();
        }

        static void Letter(Pen d, string text, PointF c, float size)
        {
            d.Canvas.FontColor = d.Ink;
            d.Canvas.FontSize = size;
            d.Canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
            d.Canvas.DrawString(text, c.X - 10, c.Y - 8, 20, 16, HorizontalAlignment.Center, VerticalAlignment.Center);
        }

        static PathF Needle(PointF baseCentre, float length, bool up)
        {
            var sign = up ? -1 : 1;
            var path = new PathF();
            path.MoveTo(baseCentre.X - 3.5f, baseCentre.Y);
            path.LineTo(baseCentre.X, baseCentre.Y + sign * length);
            path.LineTo(baseCentre.X + 3.5f, baseCentre.Y);
            path.Close();
            return path;
        }

        // ---------- the travel symbols (about size points across) ----------

        // A plane from the side, flying right.
        static void Plane(Pen d, PointF c, float size)
        {
            var s = size / 24f;
            var p = new PathF();
            p.MoveTo(c.X + 11 * s, c.Y - 1 * s);
            p.CurveTo(c.X + 12.5f * s, c.Y - 1 * s, c.X + 12.5f * s, c.Y + 1.5f * s, c.X + 11 * s, c.Y + 1.5f * s);
            p.LineTo(c.X + 2 * s, c.Y + 1.5f * s);
            p.LineTo(c.X - 4 * s, c.Y + 9 * s);
            p.LineTo(c.X - 7 * s, c.Y + 9 * s);
            p.LineTo(c.X - 3 * s, c.Y + 1.5f * s);
            p.LineTo(c.X - 8 * s, c.Y + 1.5f * s);
            p.LineTo(c.X - 10.5f * s, c.Y + 4 * s);
            p.LineTo(c.X - 12 * s, c.Y + 4 * s);
            p.LineTo(c.X - 10.5f * s, c.Y - 1 * s);
            p.LineTo(c.X - 12 * s, c.Y - 6 * s);
            p.LineTo(c.X - 10.5f * s, c.Y - 6 * s);
            p.LineTo(c.X - 8 * s, c.Y - 1 * s);
            p.LineTo(c.X - 3 * s, c.Y - 1 * s);
            p.LineTo(c.X - 7 * s, c.Y - 9 * s);
            p.LineTo(c.X - 4 * s, c.Y - 9 * s);
            p.LineTo(c.X + 2 * s, c.Y - 1 * s);
            p.Close();
            d.Canvas.FillColor = d.Ink;
            d.Canvas.FillPath(p);
        }

        static void Car(Pen d, PointF c, float size)
        {
            var s = size / 24f;
            var canvas = d.Canvas;
            canvas.FillColor = d.Ink;
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
                canvas.FillColor = d.Ink;
                canvas.FillCircle(x, c.Y + 5.5f * s, 3.2f * s);
                canvas.FillColor = Paper;
                canvas.FillCircle(x, c.Y + 5.5f * s, 1.2f * s);
            }
        }

        static void Ship(Pen d, PointF c, float size)
        {
            var s = size / 24f;
            var canvas = d.Canvas;
            canvas.FillColor = d.Ink;
            var hull = new PathF();
            hull.MoveTo(c.X - 12 * s, c.Y + 1 * s);
            hull.LineTo(c.X + 12 * s, c.Y + 1 * s);
            hull.LineTo(c.X + 8 * s, c.Y + 7 * s);
            hull.LineTo(c.X - 9 * s, c.Y + 7 * s);
            hull.Close();
            canvas.FillPath(hull);
            canvas.FillRectangle(c.X - 6 * s, c.Y - 4 * s, 11 * s, 5 * s);
            canvas.FillRectangle(c.X - 1 * s, c.Y - 9 * s, 3.5f * s, 5 * s);
            d.Stroke(1.2f * s + 0.4f, () =>
            {
                var wave = new PathF();
                wave.MoveTo(c.X - 12 * s, c.Y + 10 * s);
                wave.QuadTo(c.X - 9 * s, c.Y + 8 * s, c.X - 6 * s, c.Y + 10 * s);
                wave.QuadTo(c.X - 3 * s, c.Y + 12 * s, c.X, c.Y + 10 * s);
                wave.QuadTo(c.X + 3 * s, c.Y + 8 * s, c.X + 6 * s, c.Y + 10 * s);
                wave.QuadTo(c.X + 9 * s, c.Y + 12 * s, c.X + 12 * s, c.Y + 10 * s);
                canvas.DrawPath(wave);
            });
        }

        static void Train(Pen d, PointF c, float size)
        {
            var s = size / 24f;
            var canvas = d.Canvas;
            canvas.FillColor = d.Ink;
            canvas.FillRoundedRectangle(c.X - 7 * s, c.Y - 11 * s, 14 * s, 17 * s, 4 * s);
            canvas.FillColor = Paper;
            canvas.FillRoundedRectangle(c.X - 5 * s, c.Y - 8.5f * s, 10 * s, 6 * s, 1.5f * s);
            canvas.FillCircle(c.X - 3.5f * s, c.Y + 2 * s, 1.4f * s);
            canvas.FillCircle(c.X + 3.5f * s, c.Y + 2 * s, 1.4f * s);
            d.Stroke(1.4f * s + 0.4f, () =>
            {
                canvas.DrawLine(c.X - 5 * s, c.Y + 6 * s, c.X - 8 * s, c.Y + 11 * s);
                canvas.DrawLine(c.X + 5 * s, c.Y + 6 * s, c.X + 8 * s, c.Y + 11 * s);
                canvas.DrawLine(c.X - 9 * s, c.Y + 11 * s, c.X + 9 * s, c.Y + 11 * s);
            });
        }

        static void Walker(Pen d, PointF c, float size)
        {
            var s = size / 24f;
            var canvas = d.Canvas;
            canvas.FillColor = d.Ink;
            canvas.FillCircle(c.X + 1 * s, c.Y - 9.5f * s, 2.6f * s);
            d.Stroke(2.6f * s + 0.3f, () =>
            {
                canvas.DrawLine(c.X + 0.5f * s, c.Y - 5.5f * s, c.X - 1 * s, c.Y + 2.5f * s);
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
            });
        }

        // ---------- shapes ----------

        // A true circle unless the text (large text sizes) makes the stamp taller than wide; it then
        // stretches upright so the text stays inside.
        static RectF Round(RectF r)
        {
            if (r.Height > r.Width * 1.05f) return r;
            var size = Math.Min(r.Width, r.Height);
            return new RectF(r.Center.X - size / 2, r.Center.Y - size / 2, size, size);
        }

        // Standing on a point, with upright sides.
        static PathF Hexagon(RectF r)
        {
            var dy = r.Height * 0.2f;
            var path = new PathF();
            path.MoveTo(r.Center.X, r.Top);
            path.LineTo(r.Right, r.Top + dy);
            path.LineTo(r.Right, r.Bottom - dy);
            path.LineTo(r.Center.X, r.Bottom);
            path.LineTo(r.Left, r.Bottom - dy);
            path.LineTo(r.Left, r.Top + dy);
            path.Close();
            return path;
        }

        // An ellipse with rounded bumps all the way round.
        static PathF Scalloped(RectF r, int count, float depth)
        {
            var c = r.Center;
            var rx = r.Width / 2 - depth;
            var ry = r.Height / 2 - depth;
            var path = new PathF();
            for (var i = 0; i <= count; i++)
            {
                var a = i * MathF.PI * 2 / count;
                var point = new PointF(c.X + rx * MathF.Cos(a), c.Y + ry * MathF.Sin(a));
                if (i == 0)
                {
                    path.MoveTo(point);
                    continue;
                }
                var mid = a - MathF.PI / count;
                var control = new PointF(c.X + (rx + depth * 2) * MathF.Cos(mid), c.Y + (ry + depth * 2) * MathF.Sin(mid));
                path.QuadTo(control, point);
            }
            path.Close();
            return path;
        }

        // A ticket: corners cut in with quarter circles and a half circle out of each side.
        static PathF TicketEdge(RectF r, float corner, float notch)
        {
            var path = new PathF();
            var midY = r.Center.Y;
            path.MoveTo(r.Left + corner, r.Top);
            path.LineTo(r.Right - corner, r.Top);
            path.QuadTo(r.Right - corner, r.Top + corner, r.Right, r.Top + corner);
            path.LineTo(r.Right, midY - notch);
            path.QuadTo(r.Right - notch * 1.4f, midY, r.Right, midY + notch);
            path.LineTo(r.Right, r.Bottom - corner);
            path.QuadTo(r.Right - corner, r.Bottom - corner, r.Right - corner, r.Bottom);
            path.LineTo(r.Left + corner, r.Bottom);
            path.QuadTo(r.Left + corner, r.Bottom - corner, r.Left, r.Bottom - corner);
            path.LineTo(r.Left, midY + notch);
            path.QuadTo(r.Left + notch * 1.4f, midY, r.Left, midY - notch);
            path.LineTo(r.Left, r.Top + corner);
            path.QuadTo(r.Left + corner, r.Top + corner, r.Left + corner, r.Top);
            path.Close();
            return path;
        }

        // A triangle with softened corners; top moves the apex down for an inner line.
        static PathF TriangleEdge(RectF r, float top = 0)
        {
            var apex = new PointF(r.Center.X, r.Top + top);
            var left = new PointF(r.Left, r.Bottom);
            var right = new PointF(r.Right, r.Bottom);
            static PointF Toward(PointF from, PointF to, float t) => new(from.X + (to.X - from.X) * t, from.Y + (to.Y - from.Y) * t);
            const float t = 0.06f;
            var path = new PathF();
            path.MoveTo(Toward(apex, left, t));
            path.QuadTo(apex, Toward(apex, right, t));
            path.LineTo(Toward(right, apex, t * 0.6f));
            path.QuadTo(right, Toward(right, left, t * 0.6f));
            path.LineTo(Toward(left, right, t * 0.6f));
            path.QuadTo(left, Toward(left, apex, t * 0.6f));
            path.Close();
            return path;
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

        // Where a rubber stamp did not take the ink: small paper-coloured specks over the lines.
        static void InkGaps(ICanvas canvas, RectF bounds, Random random)
        {
            canvas.FillColor = Paper.WithAlpha(0.85f);
            var count = (int)(bounds.Width * bounds.Height / 200);
            for (var i = 0; i < count; i++)
            {
                var x = bounds.Left + (float)random.NextDouble() * bounds.Width;
                var y = bounds.Top + (float)random.NextDouble() * bounds.Height;
                canvas.FillCircle(x, y, 0.5f + (float)random.NextDouble() * 1.1f);
            }
        }

        // The canvas with the stamp's ink and line widths.
        sealed class Pen(ICanvas canvas, Color ink, bool small)
        {
            public ICanvas Canvas { get; } = canvas;
            public Color Ink { get; } = ink;
            public bool Small { get; } = small;
            public float Outer { get; } = small ? 1.6f : 3.5f;
            public float Inner { get; } = small ? 0.8f : 1.5f;
            public float Gap { get; } = small ? 2.5f : 5f;

            public void FillTint(Action fill)
            {
                Canvas.FillColor = Ink.WithAlpha(0.05f);
                fill();
            }

            public void Stroke(float width, Action draw)
            {
                Canvas.StrokeColor = Ink;
                Canvas.StrokeSize = width;
                Canvas.StrokeDashPattern = null;
                draw();
            }

            public void Dashed(float width, Action draw)
            {
                Canvas.StrokeColor = Ink;
                Canvas.StrokeSize = width;
                Canvas.StrokeDashPattern = [3, 2.5f];
                draw();
                Canvas.StrokeDashPattern = null;
            }
        }
    }
}
