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
                case StampPattern.GlobeSeal: GlobeSeal(d, r); break;
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
                case StampPattern.Orbit: Orbit(d, r); break;
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

        // A double ring with a globe at the top, sparkles at the sides and the travel row at the bottom.
        static void GlobeSeal(Pen d, RectF r)
        {
            var ring = Round(r);
            d.FillTint(() => d.Canvas.FillEllipse(ring));
            d.Stroke(d.Outer, () => d.Canvas.DrawEllipse(ring));
            d.Stroke(d.Inner, () => d.Canvas.DrawEllipse(ring.Inflate(-d.Gap, -d.Gap)));
            if (d.Small) return;
            d.Dashed(d.Inner, () => d.Canvas.DrawEllipse(ring.Inflate(-d.Gap - 9, -d.Gap - 9)));
            Globe(d, new PointF(ring.Center.X, ring.Top + 26), 14);
            Sparkle(d, new PointF(ring.Left + 18, ring.Center.Y), 5);
            Sparkle(d, new PointF(ring.Right - 18, ring.Center.Y), 5);
            TravelRow(d, new PointF(ring.Center.X, ring.Bottom - 27), 12, 15);
        }

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
            Waves(d, new RectF(inner.Center.X + 6, inner.Top + 4, inner.Right - inner.Center.X - 12, 16), 4);
            Globe(d, new PointF(inner.Center.X, inner.Bottom - 15), 10);
            d.Stroke(d.Inner, () =>
            {
                d.Canvas.DrawLine(inner.Left + 12, inner.Bottom - 15, inner.Center.X - 16, inner.Bottom - 15);
                d.Canvas.DrawLine(inner.Center.X + 16, inner.Bottom - 15, inner.Right - 12, inner.Bottom - 15);
            });
        }

        // A hexagon standing on a point, a compass rose at the top and the travel row at the bottom.
        static void CompassHexagon(Pen d, RectF r)
        {
            var outer = Hexagon(r);
            d.FillTint(() => d.Canvas.FillPath(outer));
            d.Stroke(d.Outer, () => d.Canvas.DrawPath(outer));
            d.Stroke(d.Inner, () => d.Canvas.DrawPath(Hexagon(r.Inflate(-d.Gap, -d.Gap))));
            if (d.Small) return;
            d.Dashed(d.Inner, () => d.Canvas.DrawPath(Hexagon(r.Inflate(-d.Gap - 9, -d.Gap - 9))));
            CompassRose(d, new PointF(r.Center.X, r.Top + 34), 17, letters: true);
            Sparkle(d, new PointF(r.Left + 16, r.Center.Y), 4.5f);
            Sparkle(d, new PointF(r.Right - 16, r.Center.Y), 4.5f);
            TravelRow(d, new PointF(r.Center.X, r.Bottom - 36), 11, 14);
            Sparkle(d, new PointF(r.Center.X, r.Bottom - 14), 3.5f);
        }

        // A ring with the sun's rays at the top, sparkles above and below, and postmark waves at the right.
        static void Sunrise(Pen d, RectF r)
        {
            var ring = Round(r);
            d.FillTint(() => d.Canvas.FillEllipse(ring));
            d.Stroke(d.Outer, () => d.Canvas.DrawEllipse(ring));
            d.Stroke(d.Inner, () => d.Canvas.DrawEllipse(ring.Inflate(-d.Gap, -d.Gap)));
            if (d.Small) return;
            var c = ring.Center;
            // The sun rising over a line, its rays fanning out
            var sun = new PointF(c.X, ring.Top + 34);
            d.Canvas.FillColor = d.Ink;
            var disc = new PathF();
            disc.MoveTo(sun.X - 7, sun.Y);
            disc.CurveTo(sun.X - 7, sun.Y - 9.3f, sun.X + 7, sun.Y - 9.3f, sun.X + 7, sun.Y);
            disc.Close();
            d.Canvas.FillPath(disc);
            d.Stroke(d.Inner, () =>
            {
                d.Canvas.DrawLine(sun.X - 20, sun.Y + 1, sun.X + 20, sun.Y + 1);
                for (var i = 0; i <= 8; i++)
                {
                    var angle = MathF.PI + i * MathF.PI / 8;
                    var to = i % 2 == 0 ? 19f : 15f;
                    d.Canvas.DrawLine(sun.X + 10 * MathF.Cos(angle), sun.Y + 10 * MathF.Sin(angle),
                                      sun.X + to * MathF.Cos(angle), sun.Y + to * MathF.Sin(angle));
                }
            });
            Sparkle(d, new PointF(c.X, ring.Bottom - 16), 6);
            Sparkle(d, new PointF(ring.Left + 17, c.Y), 4);
            Waves(d, new RectF(ring.Right - 26, c.Y - 18, 24, 30), 4);
        }

        // A scalloped edge round a big globe at the top, sparkles at the sides and the travel row below.
        static void ScallopedGlobe(Pen d, RectF r)
        {
            var edge = Scalloped(r, d.Small ? 14 : 26, d.Small ? 1.5f : 4f);
            d.FillTint(() => d.Canvas.FillPath(edge));
            d.Stroke(d.Outer, () => d.Canvas.DrawPath(edge));
            var ring = r.Inflate(-d.Gap * 1.6f, -d.Gap * 1.6f);
            d.Stroke(d.Inner, () => d.Canvas.DrawEllipse(ring));
            if (d.Small) return;
            Globe(d, new PointF(ring.Center.X, ring.Top + 25), 18, lines: 4);
            Sparkle(d, new PointF(ring.Left + 15, ring.Center.Y), 4.5f);
            Sparkle(d, new PointF(ring.Right - 15, ring.Center.Y), 4.5f);
            TravelRow(d, new PointF(ring.Center.X, ring.Bottom - 22), 11, 14);
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
            Waves(d, new RectF(inner.Left + 14, inner.Center.Y + 14, 22, 14), 3);
            Waves(d, new RectF(inner.Right - 36, inner.Center.Y + 14, 22, 14), 3);
            Sparkle(d, new PointF(inner.Center.X, inner.Bottom - 9), 3.5f);
        }

        // A ticket: notched corners and sides, stars and a globe at the top, the travel row below.
        static void Ticket(Pen d, RectF r)
        {
            var edge = TicketEdge(r, d.Small ? 3 : 8, d.Small ? 2.5f : 6);
            d.FillTint(() => d.Canvas.FillPath(edge));
            d.Stroke(d.Outer, () => d.Canvas.DrawPath(edge));
            var inner = r.Inflate(-d.Gap * 1.8f, -d.Gap * 1.8f);
            d.Stroke(d.Inner, () => d.Canvas.DrawRectangle(inner));
            if (d.Small) return;
            Globe(d, new PointF(inner.Center.X, inner.Top + 15), 10);
            foreach (var x in new[] { -42f, -28f, 28f, 42f })
            {
                d.Canvas.FillColor = d.Ink;
                d.Canvas.FillPath(Star(new PointF(inner.Center.X + x, inner.Top + 15), 4.5f));
            }
            d.Dashed(d.Inner, () => d.Canvas.DrawLine(inner.Left + 8, inner.Bottom - 27, inner.Right - 8, inner.Bottom - 27));
            TravelRow(d, new PointF(inner.Center.X, inner.Bottom - 13), 12, 15);
        }

        // A compass card: the ring with tick marks, a needle at the top, the four points round the edge.
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
            d.Stroke(d.Inner, () =>
            {
                for (var i = 0; i < 72; i++)
                {
                    var angle = i * MathF.PI * 2 / 72;
                    var length = i % 9 == 0 ? 7f : 3.5f;
                    var (cos, sin) = (MathF.Cos(angle), MathF.Sin(angle));
                    d.Canvas.DrawLine(c.X + rx * cos, c.Y + ry * sin, c.X + (rx - length) * cos, c.Y + (ry - length) * sin);
                }
            });
            Letter(d, "N", new PointF(c.X, inner.Top + 13), 11);
            Letter(d, "S", new PointF(c.X, inner.Bottom - 13), 11);
            Letter(d, "W", new PointF(inner.Left + 14, c.Y), 11);
            Letter(d, "E", new PointF(inner.Right - 14, c.Y), 11);
            // The needle, pointing at the N above the text
            d.Canvas.FillColor = d.Ink;
            d.Canvas.FillPath(Needle(new PointF(c.X, inner.Top + 36), 14, up: true));
        }

        // A postcard: a frame, postmark waves at the top right and bottom left, and address lines.
        static void Postcard(Pen d, RectF r)
        {
            d.FillTint(() => d.Canvas.FillRoundedRectangle(r, 10));
            d.Stroke(d.Outer, () => d.Canvas.DrawRoundedRectangle(r, 10));
            var inner = r.Inflate(-d.Gap, -d.Gap);
            d.Stroke(d.Inner, () => d.Canvas.DrawRoundedRectangle(inner, 7));
            if (d.Small) return;
            Waves(d, new RectF(inner.Center.X + 4, inner.Top + 5, inner.Right - inner.Center.X - 10, 20), 4);
            Waves(d, new RectF(inner.Left + 6, inner.Bottom - 25, inner.Width * 0.38f, 20), 4);
            d.Stroke(d.Inner, () =>
            {
                for (var i = 0; i < 3; i++)
                {
                    var y = inner.Top + 8 + i * 6;
                    d.Canvas.DrawLine(inner.Left + 8, y, inner.Left + 8 + inner.Width * 0.3f, y);
                    var y2 = inner.Bottom - 20 + i * 6;
                    d.Canvas.DrawLine(inner.Right - 8 - inner.Width * 0.3f, y2, inner.Right - 8, y2);
                }
            });
        }

        // A triangle with a globe at its top, sparkles beside the text and the travel row along the base.
        static void Triangle(Pen d, RectF r)
        {
            var outer = TriangleEdge(r);
            d.FillTint(() => d.Canvas.FillPath(outer));
            d.Stroke(d.Outer, () => d.Canvas.DrawPath(outer));
            var inner = TriangleEdge(r.Inflate(-d.Gap * 1.6f, -d.Gap * 1.6f), top: d.Gap * 1.2f);
            d.Stroke(d.Inner, () => d.Canvas.DrawPath(inner));
            if (d.Small) return;
            Globe(d, new PointF(r.Center.X, r.Top + r.Height * 0.2f + 6), 13);
            Sparkle(d, new PointF(r.Center.X - 30, r.Top + r.Height * 0.48f), 3.5f);
            Sparkle(d, new PointF(r.Center.X + 30, r.Top + r.Height * 0.48f), 3.5f);
            TravelRow(d, new PointF(r.Center.X, r.Bottom - 20), 11, 15);
        }

        // A seal with a pinked edge, a compass star at the top and sparkles at the sides.
        static void StarSeal(Pen d, RectF r)
        {
            var edge = Scalloped(r, d.Small ? 18 : 40, d.Small ? 1.2f : 2.5f);
            d.FillTint(() => d.Canvas.FillPath(edge));
            d.Stroke(d.Inner * 1.4f, () => d.Canvas.DrawPath(edge));
            var ring = r.Inflate(-d.Gap * 1.4f, -d.Gap * 1.4f);
            d.Stroke(d.Inner, () => d.Canvas.DrawEllipse(ring));
            if (d.Small) return;
            d.Dashed(d.Inner, () => d.Canvas.DrawEllipse(ring.Inflate(-7, -7)));
            CompassRose(d, new PointF(ring.Center.X, ring.Top + 24), 13, letters: false);
            Sparkle(d, new PointF(ring.Left + 15, ring.Center.Y), 4.5f);
            Sparkle(d, new PointF(ring.Right - 15, ring.Center.Y), 4.5f);
            Sparkle(d, new PointF(ring.Center.X, ring.Bottom - 16), 5.5f);
        }

        // A round-cornered frame with the world map at the top and routes circling round the text.
        static void Orbit(Pen d, RectF r)
        {
            d.FillTint(() => d.Canvas.FillRoundedRectangle(r, 14));
            d.Stroke(d.Outer, () => d.Canvas.DrawRoundedRectangle(r, 14));
            var inner = r.Inflate(-d.Gap, -d.Gap);
            if (d.Small) return;
            WorldMap(d, new RectF(inner.Left + inner.Width * 0.22f, inner.Top + 6, inner.Width * 0.56f, 34), clearCentre: 0);
            var orbit = new RectF(inner.Left + 4, inner.Top + 30, inner.Width - 8, inner.Height - 50);
            d.Stroke(d.Inner, () =>
            {
                d.Canvas.StrokeColor = d.Ink.WithAlpha(0.55f);
                d.Canvas.DrawEllipse(orbit);
            });
            d.Canvas.FillColor = d.Ink;
            for (var i = 0; i < 8; i++)
            {
                var angle = i * MathF.PI / 4 + 0.3f;
                d.Canvas.FillCircle(orbit.Center.X + orbit.Width / 2 * MathF.Cos(angle), orbit.Center.Y + orbit.Height / 2 * MathF.Sin(angle), 2.2f);
            }
            d.Stroke(d.Inner, () =>
            {
                d.Canvas.DrawLine(inner.Left + 18, inner.Bottom - 12, inner.Center.X - 14, inner.Bottom - 12);
                d.Canvas.DrawLine(inner.Center.X + 14, inner.Bottom - 12, inner.Right - 18, inner.Bottom - 12);
            });
            Sparkle(d, new PointF(inner.Center.X, inner.Bottom - 12), 5);
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
        static void CompassRose(Pen d, PointF c, float radius, bool letters)
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

        // Every way of travelling in a row: plane, ship, train, car and on foot.
        static void TravelRow(Pen d, PointF centre, float size, float spacing)
        {
            var symbols = new Action<PointF>[]
            {
                p => Plane(d, p, size),
                p => Ship(d, p, size),
                p => Train(d, p, size),
                p => Car(d, p, size),
                p => Walker(d, p, size),
            };
            var start = centre.X - spacing * (symbols.Length - 1) / 2f;
            for (var i = 0; i < symbols.Length; i++)
            {
                symbols[i](new PointF(start + i * spacing, centre.Y));
            }
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
