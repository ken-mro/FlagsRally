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
                case StampPattern.Triangle: Triangle(d, r); break;
                case StampPattern.GlobePostmark: GlobePostmark(d, r); break;
                case StampPattern.Journey: Journey(d, r); break;
                case StampPattern.StarSeal: StarSeal(d, r); break;
                default:
                    // The original stamp: a thick square with a dashed one inside
                    var square = bounds.Inflate(-2, -2);
                    d.FillTint(() => canvas.FillRectangle(square));
                    d.Stroke(d.Small ? 1.6f : 4, () => canvas.DrawRectangle(square));
                    d.Stroke(d.Small ? 0.8f : 2, () =>
                    {
                        canvas.StrokeDashPattern = [1, 1]; // dashes as long as the line is thick, as before
                        canvas.DrawRectangle(square.Inflate(-5, -5));
                        canvas.StrokeDashPattern = null;
                    });
                    break;
            }

            if (!frame.IsSmall)
            {
                InkGaps(canvas, bounds, new Random(frame.Seed * 31 + (int)frame.Pattern));
            }
        }

        // ---------- the designs ----------

        // An airmail envelope: the striped edge, at the top right a postage stamp cancelled by a round
        // postmark whose wavy lines run off to the left, and an address line under the text, which
        // lines up at the left (see StampPatterns.IsLeftAligned).
        static void Airmail(Pen d, RectF r)
        {
            d.FillTint(() => d.Canvas.FillRoundedRectangle(r, 8));
            d.Stroke(d.Inner, () => d.Canvas.DrawRoundedRectangle(r, 8));
            var stripe = d.Small ? 2.5f : 6f;
            AirmailStripes(d, r.Inflate(-d.Inner, -d.Inner), stripe);
            var inner = r.Inflate(-stripe - d.Inner * 2, -stripe - d.Inner * 2);
            d.Stroke(d.Inner, () => d.Canvas.DrawRectangle(inner));
            if (d.Small) return;

            // The postage stamp: a perforated paper square with a frame and a little mountain and sun
            var stamp = new RectF(inner.Right - 27, inner.Top + 5, 22, 26);
            var edge = Perforated(stamp, 3.2f);
            d.Canvas.FillColor = Color.FromArgb("#FFFDF7");
            d.Canvas.FillPath(edge);
            d.Stroke(d.Inner * 0.7f, () => d.Canvas.DrawPath(edge));
            var picture = stamp.Inflate(-3.5f, -3.5f);
            d.Stroke(d.Inner * 0.7f, () => d.Canvas.DrawRectangle(picture));
            var hill = new PathF();
            hill.MoveTo(picture.Left, picture.Bottom);
            hill.LineTo(picture.Left + picture.Width * 0.38f, picture.Top + picture.Height * 0.45f);
            hill.LineTo(picture.Left + picture.Width * 0.62f, picture.Top + picture.Height * 0.7f);
            hill.LineTo(picture.Left + picture.Width * 0.78f, picture.Top + picture.Height * 0.55f);
            hill.LineTo(picture.Right, picture.Bottom);
            hill.Close();
            d.Canvas.FillColor = d.Ink;
            d.Canvas.FillPath(hill);
            d.Canvas.FillCircle(picture.Right - 4, picture.Top + 4, 2);

            // The postmark over the stamp's lower left corner, and its waves to the left
            var mark = new PointF(stamp.Left - 2, stamp.Bottom - 4);
            d.Stroke(d.Inner * 0.9f, () =>
            {
                d.Canvas.DrawCircle(mark, 10);
                d.Canvas.DrawCircle(mark, 7.5f);
            });
            d.Canvas.FillColor = d.Ink;
            d.Canvas.FillPath(StarPoints(mark, 4, 4));
            Waves(d, new RectF(mark.X - 40, mark.Y - 6, 28, 12), 3);

            // The address line under the text, which lines up at the left like an address
            d.Stroke(d.Inner * 0.8f, () =>
                d.Canvas.DrawLine(inner.Left + 10, inner.Bottom - 9, inner.Left + inner.Width * 0.66f, inner.Bottom - 9));
        }

        // A hexagon on its point: a compass rose in a ring with N, W and E at the top, a dashed orbit
        // round the text with a plane flying along its upper left, sparkles at the sides, and ship,
        // train, car and someone on foot along the bottom.
        static void CompassHexagon(Pen d, RectF r)
        {
            var outer = Hexagon(r);
            d.FillTint(() => d.Canvas.FillPath(outer));
            d.Stroke(d.Outer, () => d.Canvas.DrawPath(outer));
            d.Stroke(d.Inner, () => d.Canvas.DrawPath(Hexagon(r.Inflate(-d.Gap, -d.Gap))));
            if (d.Small) return;
            var c = r.Center;
            var hexWidth = Math.Min(r.Width, r.Height * 0.866f);
            var orbit = new RectF(c.X - hexWidth / 2 + 15, r.Top + 30, hexWidth - 30, r.Height - 58);
            // Gaps for the plane and the rose at the top; the bottom is left to the date and the travel row
            Orbit(d, orbit, (140, 205), (245, 252), (288, 400));
            CompassRose(d, new PointF(c.X, r.Top + 30), 12, letters: false, ring: true);
            Sparkle(d, new PointF(c.X - hexWidth / 2 + 10, c.Y), 4.5f);
            Sparkle(d, new PointF(c.X + hexWidth / 2 - 10, c.Y), 4.5f);
            TravelRow(d, new PointF(c.X, r.Bottom - 30), 12, 20, TravelMode.Ship, TravelMode.Train, TravelMode.Car, TravelMode.Walk);
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

        // A round medal: a solid band of ink with a bumpy edge, a thin ring inside it, a globe at the top
        // with stars on an arc either side, laurel branches up both sides and a large star at the bottom.
        static void ScallopedGlobe(Pen d, RectF r)
        {
            // A solid band of ink between the scalloped edge and a ring, round a true circle
            var medal = Round(r);
            var edge = Scalloped(medal, d.Small ? 12 : 22, d.Small ? 1.5f : 3.5f);
            var ring = medal.Inflate(-(d.Small ? 3 : 8), -(d.Small ? 3 : 8));
            d.Canvas.FillColor = d.Ink;
            d.Canvas.FillPath(edge);
            // The paper shows again inside the ring
            d.Canvas.FillColor = Paper;
            d.Canvas.FillEllipse(ring);
            d.FillTint(() => d.Canvas.FillEllipse(ring));
            if (d.Small) return;
            var inner = ring.Inflate(-4, -4);
            d.Stroke(d.Inner * 0.8f, () => d.Canvas.DrawEllipse(inner));
            var c = inner.Center;
            var rx = inner.Width / 2;
            var ry = inner.Height / 2;
            PointF On(float degrees, float inset)
            {
                var a = degrees * MathF.PI / 180;
                return new PointF(c.X + (rx - inset) * MathF.Cos(a), c.Y + (ry - inset) * MathF.Sin(a));
            }
            Globe(d, new PointF(c.X, inner.Top + 17), 10);
            d.Canvas.FillColor = d.Ink;
            foreach (var angle in new[] { 233f, 250f, 290f, 307f })
            {
                d.Canvas.FillPath(Star(On(angle, 12), 4.2f));
            }
            // Up the sides beside the flag and the name, clear of the date
            Laurel(d, c, rx - 11, ry - 11, 156, 222, left: true);
            Laurel(d, c, rx - 11, ry - 11, 24, -42, left: false);
            var bottom = On(90, 12);
            d.Canvas.FillColor = d.Ink;
            d.Canvas.FillPath(Star(bottom, 7));
            d.Stroke(d.Inner, () =>
            {
                d.Canvas.DrawLine(bottom.X - 24, bottom.Y + 1, bottom.X - 12, bottom.Y + 1);
                d.Canvas.DrawLine(bottom.X + 12, bottom.Y + 1, bottom.X + 24, bottom.Y + 1);
            });
        }

        // A world map's outline: the oval of an equal-area projection, with its curved meridians and
        // straight parallels round the edge, a clear oval window in the middle for the text, and
        // sparkles at the sides.
        static void WorldOval(Pen d, RectF r)
        {
            d.FillTint(() => d.Canvas.FillEllipse(r));
            d.Stroke(d.Outer, () => d.Canvas.DrawEllipse(r));
            var inner = r.Inflate(-d.Gap, -d.Gap);
            d.Stroke(d.Inner, () => d.Canvas.DrawEllipse(inner));
            if (d.Small) return;
            var c = inner.Center;
            var rx = inner.Width / 2;
            var ry = inner.Height / 2;
            // The grid round the edge only; an oval window in the middle is left clear for the text
            d.Canvas.SaveState();
            var window = new PathF();
            window.AppendEllipse(inner);
            window.AppendEllipse(new RectF(c.X - rx * 0.7f, c.Y - ry * 0.9f, rx * 1.4f, ry * 1.8f));
            d.Canvas.ClipPath(window, WindingMode.EvenOdd);
            d.Canvas.StrokeColor = d.Ink.WithAlpha(0.55f);
            d.Canvas.StrokeSize = d.Inner * 0.8f;
            d.Canvas.StrokeDashPattern = null;
            // Meridians: half ellipses from pole to pole, narrower toward the middle
            foreach (var share in new[] { 0.25f, 0.5f, 0.75f })
            {
                var w = rx * share;
                d.Canvas.DrawEllipse(c.X - w, inner.Top, w * 2, inner.Height);
            }
            d.Canvas.DrawLine(c.X, inner.Top, c.X, inner.Bottom);
            // Parallels: straight lines across the oval
            foreach (var f in new[] { -0.66f, -0.33f, 0f, 0.33f, 0.66f })
            {
                var y = c.Y + f * ry;
                var half = rx * MathF.Sqrt(1 - f * f);
                d.Canvas.DrawLine(c.X - half, y, c.X + half, y);
            }
            d.Canvas.RestoreState();
            Sparkle(d, new PointF(inner.Left + 12, c.Y), 4.5f);
            Sparkle(d, new PointF(inner.Right - 12, c.Y), 4.5f);
        }

        // A ticket: a thick edge with corners cut in and a notch in each side, a thin line inside it,
        // stars round a globe at the top, speed lines either side of the notches, and below a line the
        // row of ways to travel.
        static void Ticket(Pen d, RectF r)
        {
            var edge = TicketEdge(r, d.Small ? 4 : 12, d.Small ? 3 : 10, 0);
            d.FillTint(() => d.Canvas.FillPath(edge));
            d.Stroke(d.Outer, () => d.Canvas.DrawPath(edge));
            if (d.Small) return;
            var inset = d.Gap * 1.3f;
            var inner = r.Inflate(-inset, -inset);
            d.Stroke(d.Inner, () => d.Canvas.DrawPath(TicketEdge(r, 12, 10, 0, inset)));
            Globe(d, new PointF(inner.Center.X, inner.Top + 16), 11);
            d.Canvas.FillColor = d.Ink;
            foreach (var x in new[] { -46f, -30f, 30f, 46f })
            {
                d.Canvas.FillPath(Star(new PointF(inner.Center.X + x, inner.Top + 16), 5.5f));
            }
            // Speed lines either side of the notches, inside the thin line
            d.Stroke(d.Inner * 0.8f, () =>
            {
                foreach (var dy in new[] { -24f, -19f, -14f, 14f, 19f, 24f })
                {
                    var y = inner.Center.Y + dy;
                    d.Canvas.DrawLine(inner.Left + 3, y, inner.Left + 10, y);
                    d.Canvas.DrawLine(inner.Right - 10, y, inner.Right - 3, y);
                }
            });
            var line = inner.Bottom - 30;
            d.Stroke(d.Inner, () => d.Canvas.DrawLine(inner.Left + 2, line, inner.Right - 2, line));
            TravelRow(d, new PointF(inner.Center.X, inner.Bottom - 15), 16, 26,
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

        // A triangle with a double line: a globe near the top with dashed routes curving down both
        // sides and sparkles along them. The text fills the wide lower half, so there is no travel row.
        static void Triangle(Pen d, RectF r)
        {
            var outer = TriangleEdge(r);
            d.FillTint(() => d.Canvas.FillPath(outer));
            d.Stroke(d.Outer, () => d.Canvas.DrawPath(outer));
            var inner = TriangleEdge(r, inset: d.Gap + d.Outer / 2);
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

        // A postmark: a round-cornered double frame with a big globe, its grid and continents, behind the
        // flag; rules at the top corners and beside the date, a small diamond at the bottom, and wavy
        // lines running off the right edge.
        static void GlobePostmark(Pen d, RectF r)
        {
            d.FillTint(() => d.Canvas.FillRoundedRectangle(r, 12));
            d.Stroke(d.Outer, () => d.Canvas.DrawRoundedRectangle(r, 12));
            var inner = r.Inflate(-d.Gap, -d.Gap);
            d.Stroke(d.Inner, () => d.Canvas.DrawRoundedRectangle(inner, 8));
            if (d.Small) return;
            var c = inner.Center;

            // A big globe behind the flag, its lower part cut off where the name begins
            var radius = inner.Width * 0.36f;
            var globe = new PointF(c.X, inner.Top + 8 + radius);
            var horizon = globe.Y + 2;
            d.Canvas.SaveState();
            d.Canvas.ClipRectangle(inner.Left, inner.Top, inner.Width, horizon - inner.Top);
            Graticule(d, globe, radius);
            Continents(d, globe, radius);
            d.Stroke(d.Inner * 1.3f, () => d.Canvas.DrawCircle(globe, radius));
            d.Canvas.RestoreState();

            d.Stroke(d.Inner, () =>
            {
                foreach (var y in new[] { inner.Top + 12f, inner.Top + 17f })
                {
                    d.Canvas.DrawLine(inner.Left + 8, y, inner.Left + 26, y);
                    d.Canvas.DrawLine(inner.Right - 26, y, inner.Right - 8, y);
                }
                foreach (var y in new[] { inner.Bottom - 30f, inner.Bottom - 25f })
                {
                    d.Canvas.DrawLine(inner.Left + 8, y, inner.Left + 22, y);
                    d.Canvas.DrawLine(inner.Right - 22, y, inner.Right - 8, y);
                }
                d.Canvas.DrawLine(c.X - 26, inner.Bottom - 9, c.X - 8, inner.Bottom - 9);
                d.Canvas.DrawLine(c.X + 8, inner.Bottom - 9, c.X + 26, inner.Bottom - 9);
            });
            d.Canvas.FillColor = d.Ink;
            d.Canvas.FillPath(Diamond(new PointF(c.X, inner.Bottom - 9), 2.5f));
            Waves(d, new RectF(inner.Right - 20, c.Y - 4, r.Right + d.Outer - (inner.Right - 20), 28), 4);
        }

        // A journey compass, traced from an illustration (Resources/Stamps/journey.txt): a double ring
        // with arrowheads at the four points, a dotted circle and sparkles, and inside it a rising sun
        // with clouds, a plane, a train on its track past mountains and trees, a ship on the waves,
        // a car on a road and someone walking a path, swirling round a globe under the flag. The
        // picture inside the ring is drawn a little smaller and higher, and its colours faded, leaving
        // room at the bottom for the name and the date and letting the flag stand out.
        static void Journey(Pen d, RectF r)
        {
            var ring = Round(r);
            d.Canvas.FillColor = Paper;
            d.Canvas.FillEllipse(ring);
            JourneyPicture(d, ring, part => part == 'f');
            JourneyPicture(d, ring, part => part != 'f', JourneyShrink, JourneyShrinkAbout);
        }

        // How the picture inside the ring shrinks, about a point at its top (in the 1254 box). The
        // flag's place in MainPage.xaml follows the globe's centre, (627, 654) before shrinking.
        const float JourneyShrink = 0.86f;
        static readonly PointF JourneyShrinkAbout = new(625, 140);

        // How far the picture's colours fade towards the paper, so the flag stands out
        const float JourneyFade = 0.6f;

        static Color Fade(Color colour, float amount) => new(
            colour.Red + (Paper.Red - colour.Red) * amount,
            colour.Green + (Paper.Green - colour.Green) * amount,
            colour.Blue + (Paper.Blue - colour.Blue) * amount);

        // Draws the traced shapes of the given parts into the ring, shrunk about a point when asked
        static void JourneyPicture(Pen d, RectF ring, Func<char, bool> parts, float shrink = 1, PointF about = default)
        {
            // The traced outer ring spans 22..1228 of the 1254 box
            var scale = ring.Width / 1206f;
            d.Canvas.SaveState();
            d.Canvas.Translate(ring.X - 22 * scale, ring.Y - 22 * scale);
            d.Canvas.Scale(scale, scale);
            if (shrink != 1)
            {
                d.Canvas.Translate(about.X * (1 - shrink), about.Y * (1 - shrink));
                d.Canvas.Scale(shrink, shrink);
            }
            foreach (var (colour, part, path) in JourneyShapes.Value)
            {
                if (!parts(part)) continue;
                d.Canvas.FillColor = Fade(colour ?? d.Ink, JourneyFade);
                d.Canvas.FillPath(path);
            }
            d.Canvas.RestoreState();
        }

        // Read once: about two hundred shapes. A null colour is the stamp's ink.
        static readonly Lazy<(Color? Colour, char Part, PathF Path)[]> JourneyShapes = new(() =>
        {
            var assembly = typeof(StampFrame).Assembly;
            using var stream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Resources.Stamps.journey.txt")!;
            using var reader = new StreamReader(stream);
            var shapes = new List<(Color?, char, PathF)>();
            while (reader.ReadLine() is { } line)
            {
                if (line.Length == 0 || line[0] == '#') continue;
                var parts = line.Split(' ', 3);
                Color? colour = parts[0] switch
                {
                    "ink" => null,
                    "paper" => Paper,
                    _ => Color.FromArgb("#" + parts[0]),
                };
                shapes.Add((colour, parts[1][0], PathBuilder.Build(parts[2])));
            }
            return [.. shapes];
        });

        // ---------- motifs ----------

        // A globe's faint grid: parallels and meridians inside its outline.
        static void Graticule(Pen d, PointF c, float radius)
        {
            d.Canvas.StrokeColor = d.Ink.WithAlpha(0.35f);
            d.Canvas.StrokeSize = d.Inner * 0.7f;
            d.Canvas.StrokeDashPattern = null;
            foreach (var f in new[] { -0.75f, -0.5f, -0.25f, 0f, 0.25f })
            {
                var y = c.Y + f * radius;
                var half = radius * MathF.Sqrt(1 - f * f);
                d.Canvas.DrawLine(c.X - half, y, c.X + half, y);
            }
            foreach (var share in new[] { 0.35f, 0.7f })
            {
                d.Canvas.DrawEllipse(c.X - radius * share, c.Y - radius, radius * share * 2, radius * 2);
            }
            d.Canvas.DrawLine(c.X, c.Y - radius, c.X, c.Y + radius);
        }

        // The land on a globe seen from above the Atlantic (25°N 15°W): the Americas at the left,
        // Greenland, Europe and Africa. Only the upper half, which is all the postmark shows.
        static void Continents(Pen d, PointF centre, float radius)
        {
            d.Canvas.SaveState();
            d.Canvas.Translate(centre.X, centre.Y);
            d.Canvas.Scale(radius, radius);
            d.Canvas.FillColor = d.Ink.WithAlpha(0.75f);
            d.Canvas.FillPath(GlobeLand.Value, WindingMode.EvenOdd);
            d.Canvas.RestoreState();
        }

        // Read once from Resources/Stamps/globe_land.txt
        static readonly Lazy<PathF> GlobeLand = new(() =>
        {
            var assembly = typeof(StampFrame).Assembly;
            using var stream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Resources.Stamps.globe_land.txt")!;
            using var reader = new StreamReader(stream);
            var data = string.Concat(reader.ReadToEnd().Split('\n').Where(line => !line.StartsWith('#')));
            return PathBuilder.Build(data);
        });

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

        // A globe wider than it is tall, with parallels and meridians, for the dome over a stamp's text.
        static void WideGlobe(Pen d, PointF c, float rx, float ry)
        {
            d.Stroke(d.Inner * 1.1f, () =>
            {
                d.Canvas.DrawEllipse(c.X - rx, c.Y - ry, rx * 2, ry * 2);
                foreach (var f in new[] { -0.66f, -0.33f, 0f })
                {
                    var y = c.Y + f * ry;
                    var half = rx * MathF.Sqrt(1 - f * f);
                    d.Canvas.DrawLine(c.X - half, y, c.X + half, y);
                }
                d.Canvas.DrawLine(c.X, c.Y - ry, c.X, c.Y + ry);
                foreach (var share in new[] { 0.36f, 0.7f })
                {
                    var w = rx * 2 * share;
                    d.Canvas.DrawEllipse(c.X - w / 2, c.Y - ry, w, ry * 2);
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

        // A dashed orbit: the given arcs of an ellipse (in degrees, 0 at the right, going down), with a
        // plane flying clockwise along it at the upper left, where the first gap is.
        static void Orbit(Pen d, RectF ellipse, params (float From, float To)[] arcs)
        {
            d.Dashed(d.Inner, () =>
            {
                foreach (var (from, to) in arcs)
                {
                    if (to > from) d.Canvas.DrawPath(ArcPath(ellipse, from, to));
                }
            });
            const float at = 225 * MathF.PI / 180;
            var point = new PointF(ellipse.Center.X + ellipse.Width / 2 * MathF.Cos(at), ellipse.Center.Y + ellipse.Height / 2 * MathF.Sin(at));
            // Pointing along the orbit, the way it goes round
            var heading = MathF.Atan2(ellipse.Height / 2 * MathF.Cos(at), -ellipse.Width / 2 * MathF.Sin(at)) * 180 / MathF.PI;
            d.Canvas.SaveState();
            d.Canvas.Translate(point.X, point.Y);
            d.Canvas.Rotate(heading);
            Plane(d, PointF.Zero, 17);
            d.Canvas.RestoreState();
        }

        // A laurel branch along an elliptical arc round centre, from one angle to another (degrees, 0 at
        // the right, going down): a stem with leaves on both sides, pointing toward the end of the arc.
        static void Laurel(Pen d, PointF centre, float rx, float ry, float from, float to, bool left)
        {
            PointF At(float degrees)
            {
                var a = degrees * MathF.PI / 180;
                return new PointF(centre.X + rx * MathF.Cos(a), centre.Y + ry * MathF.Sin(a));
            }
            d.Stroke(d.Inner * 0.9f, () =>
            {
                var stem = new PathF();
                for (var i = 0; i <= 20; i++)
                {
                    var p = At(from + (to - from) * i / 20f);
                    if (i == 0) stem.MoveTo(p); else stem.LineTo(p);
                }
                d.Canvas.DrawPath(stem);
            });
            d.Canvas.FillColor = d.Ink;
            const int pairs = 6;
            for (var i = 0; i < pairs; i++)
            {
                var t = from + (to - from) * (i + 0.6f) / pairs;
                var p = At(t);
                var ahead = At(t + Math.Sign(to - from) * 2);
                var heading = MathF.Atan2(ahead.Y - p.Y, ahead.X - p.X) * 180 / MathF.PI;
                var length = 7.5f - i * 0.4f;
                foreach (var side in new[] { -1, 1 })
                {
                    d.Canvas.SaveState();
                    d.Canvas.Translate(p.X, p.Y);
                    d.Canvas.Rotate(heading + side * 38);
                    var leaf = new PathF();
                    leaf.MoveTo(0, 0);
                    leaf.QuadTo(length * 0.5f, -length * 0.32f, length, 0);
                    leaf.QuadTo(length * 0.5f, length * 0.32f, 0, 0);
                    leaf.Close();
                    d.Canvas.FillPath(leaf);
                    d.Canvas.RestoreState();
                }
            }
            // A leaf at the tip
            var tip = At(to);
            var before = At(to - Math.Sign(to - from) * 2);
            d.Canvas.SaveState();
            d.Canvas.Translate(tip.X, tip.Y);
            d.Canvas.Rotate(MathF.Atan2(tip.Y - before.Y, tip.X - before.X) * 180 / MathF.PI);
            var end = new PathF();
            end.MoveTo(0, 0);
            end.QuadTo(3.5f, -2.2f, 7, 0);
            end.QuadTo(3.5f, 2.2f, 0, 0);
            end.Close();
            d.Canvas.FillPath(end);
            d.Canvas.RestoreState();
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
            // As near a regular hexagon as the stamp allows: as wide as it is tall times sin 60°
            var width = Math.Min(r.Width, r.Height * 0.866f);
            r = new RectF(r.Center.X - width / 2, r.Top, width, r.Height);
            var dy = Math.Min(r.Height * 0.25f, r.Width * 0.2887f);
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
        // A ticket's edge: quarter circles bitten out of the corners and half circles out of the sides.
        // inset draws the line that far inside it: every bite keeps its centre on the outer edge with a
        // radius larger by the inset and ends where it meets the inset sides, so the lines stay parallel.
        static PathF TicketEdge(RectF r, float corner, float notch, float notchY, float inset = 0)
        {
            var path = new PathF();
            var midY = notchY > 0 ? notchY : r.Center.Y;
            var first = true;
            void Bite(PointF centre, float radius, float from, float to)
            {
                radius += inset;
                // Trim both ends so the arc meets the inset sides instead of the outer ones
                var trim = MathF.Asin(Math.Clamp(inset / radius, 0, 1)) * 180 / MathF.PI;
                var direction = MathF.Sign(to - from);
                from += direction * trim;
                to -= direction * trim;
                const int steps = 14;
                for (var i = 0; i <= steps; i++)
                {
                    var angle = (from + (to - from) * i / steps) * MathF.PI / 180;
                    var point = new PointF(centre.X + radius * MathF.Cos(angle), centre.Y + radius * MathF.Sin(angle));
                    if (first) { path.MoveTo(point); first = false; } else path.LineTo(point);
                }
            }
            Bite(new PointF(r.Left, r.Top), corner, 90, 0);
            Bite(new PointF(r.Right, r.Top), corner, 180, 90);
            if (notch > 0) Bite(new PointF(r.Right, midY), notch, 270, 90);
            Bite(new PointF(r.Right, r.Bottom), corner, 270, 180);
            Bite(new PointF(r.Left, r.Bottom), corner, 0, -90);
            if (notch > 0) Bite(new PointF(r.Left, midY), notch, 90, -90);
            path.Close();
            return path;
        }

        // A triangle with softened corners; top moves the apex down for an inner line.
        static PathF TriangleEdge(RectF r, float inset = 0)
        {
            var apex = new PointF(r.Center.X, r.Top);
            var left = new PointF(r.Left, r.Bottom);
            var right = new PointF(r.Right, r.Bottom);
            if (inset > 0)
            {
                // Move each corner toward the incentre, so every side moves in by the same distance
                var a = Distance(left, right);
                var b = Distance(apex, right);
                var cc = Distance(apex, left);
                var perimeter = a + b + cc;
                var centre = new PointF((a * apex.X + b * left.X + cc * right.X) / perimeter, (a * apex.Y + b * left.Y + cc * right.Y) / perimeter);
                var area = MathF.Abs((left.X - apex.X) * (right.Y - apex.Y) - (right.X - apex.X) * (left.Y - apex.Y)) / 2;
                var inradius = 2 * area / perimeter;
                var k = 1 - inset / inradius;
                PointF In(PointF v) => new(centre.X + (v.X - centre.X) * k, centre.Y + (v.Y - centre.Y) * k);
                (apex, left, right) = (In(apex), In(left), In(right));
            }
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

        // A postage stamp's edge: little half-round bites all the way round.
        static PathF Perforated(RectF r, float step)
        {
            var path = new PathF();
            var depth = step * 0.45f;
            void Edge(PointF from, PointF to, PointF inward)
            {
                var count = Math.Max(1, (int)MathF.Round(Distance(from, to) / step));
                for (var i = 0; i < count; i++)
                {
                    var t1 = (float)(i + 1) / count;
                    var mid = (i + 0.5f) / count;
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

        static float Distance(PointF a, PointF b) => MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

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
