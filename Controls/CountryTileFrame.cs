using FlagsRally.Models;

namespace FlagsRally.Controls;

/// <summary>
/// The paper, motifs and frame of a visited region's tile, in its country's style
/// (<see cref="CountryTileStyles"/>): Japan's waves and Mount Fuji, America's stars and stripes,
/// Canada's maple leaves, a Paris street plaque, Bauhaus shapes, laurel, a Moorish arch, azulejo
/// corners, tulips, Swiss crosses, Klimt's gold squares, an Art Nouveau curve, Selbu stars,
/// vyshyvanka stitches and songket teeth. The tile's flag, name, date and ARRIVAL sit on top; the
/// motifs keep to the corners and edges, clear of them; the tile leaves 10 under ARRIVAL for those
/// along the bottom edge. The name band starts 54 from the top
/// (the flag's fixed box), so the arches that end on it are drawn to that height.
/// Tiles of one size look alike, so each country's is drawn once per size and reused.
/// </summary>
public class CountryTileFrame : GraphicsView
{
    public static readonly BindableProperty CountryCodeProperty = BindableProperty.Create(
        nameof(CountryCode), typeof(string), typeof(CountryTileFrame), null,
        propertyChanged: (view, _, _) => ((CountryTileFrame)view).Invalidate());

    public CountryTileFrame()
    {
        Drawable = new TileDrawable(this);
        BackgroundColor = Colors.Transparent;
        InputTransparent = true;
    }

    public string? CountryCode
    {
        get => (string?)GetValue(CountryCodeProperty);
        set => SetValue(CountryCodeProperty, value);
    }

    const float FrameWidth = 4;
    const float BandTop = 54;
    static readonly Color Paper = Color.FromArgb("#F4EEDF");

    sealed class TileDrawable(CountryTileFrame frame) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF bounds)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            var style = CountryTileStyles.For(frame.CountryCode);
#if ANDROID || IOS || MACCATALYST
            var scale = canvas.DisplayScale;
            var key = (style.CountryCode, (int)MathF.Round(bounds.Width * scale), (int)MathF.Round(bounds.Height * scale));
            if (!Pictures.TryGetValue(key, out var picture))
            {
                var context = new Microsoft.Maui.Graphics.Platform.PlatformBitmapExportContext(
                    (int)MathF.Ceiling(bounds.Width), (int)MathF.Ceiling(bounds.Height), scale);
                DrawTile(context.Canvas, new RectF(0, 0, bounds.Width, bounds.Height), style);
                picture = (context, context.Image);
                if (Pictures.Count >= 24)
                {
                    foreach (var old in Pictures.Values) old.Context.Dispose();
                    Pictures.Clear();
                }
                Pictures[key] = picture;
            }
            canvas.DrawImage(picture.Image, bounds.X, bounds.Y, bounds.Width, bounds.Height);
#else
            DrawTile(canvas, bounds, style);
#endif
        }
    }

#if ANDROID || IOS || MACCATALYST
    // A tile size or two per country: the grid's and the popup's
    static readonly Dictionary<(string Country, int Width, int Height), (Microsoft.Maui.Graphics.Platform.PlatformBitmapExportContext Context, Microsoft.Maui.Graphics.IImage Image)> Pictures = [];
#endif

    static void DrawTile(ICanvas canvas, RectF bounds, CountryTileStyle style)
    {
        var outline = bounds.Inflate(-FrameWidth / 2, -FrameWidth / 2);
        canvas.FillColor = Paper;
        canvas.FillRoundedRectangle(outline, style.Radius);

        canvas.SaveState();
        var clip = new PathF();
        clip.AppendRoundedRectangle(bounds.Inflate(-FrameWidth, -FrameWidth), MathF.Max(style.Radius - FrameWidth / 2, 0));
        canvas.ClipPath(clip);
        var t = new Tile(canvas, bounds.X, bounds.Y, bounds.Width, bounds.Height);
        switch (style.CountryCode)
        {
            case "JP": Japan(t); break;
            case "US": America(t); break;
            case "CA": Canada(t); break;
            case "FR": France(t); break;
            case "DE": Germany(t); break;
            case "IT": Italy(t); break;
            case "ES": Spain(t); break;
            case "PT": Portugal(t); break;
            case "NL": Netherlands(t); break;
            case "CH": Switzerland(t); break;
            case "AT": Austria(t); break;
            case "CZ": Czechia(t); break;
            case "NO": Norway(t); break;
            case "UA": Ukraine(t); break;
            case "MY": Malaysia(t); break;
        }
        canvas.RestoreState();

        canvas.StrokeColor = style.Frame;
        canvas.StrokeSize = FrameWidth;
        canvas.DrawRoundedRectangle(outline, style.Radius);
    }

    /// <summary>The tile's canvas with its size, in points from its top left.</summary>
    readonly record struct Tile(ICanvas Canvas, float X, float Y, float W, float H)
    {
        public float Right(float fromRight) => X + W - fromRight;
        public float Bottom(float fromBottom) => Y + H - fromBottom;
        public float Mid => X + W / 2;

        public void Fill(Color colour, PathF path)
        {
            Canvas.FillColor = colour;
            Canvas.FillPath(path);
        }

        public void Stroke(Color colour, float width, PathF path)
        {
            Canvas.StrokeColor = colour;
            Canvas.StrokeSize = width;
            Canvas.StrokeLineCap = LineCap.Round;
            Canvas.DrawPath(path);
        }
    }

    static Color C(string hex) => Color.FromArgb(hex);

    // Waves (seigaiha) along the bottom, and Mount Fuji under the sun in the top right
    static void Japan(Tile t)
    {
        var line = C("#D8DEEA");
        const float r = 11;
        var top = t.Bottom(38);
        for (var row = 0; row * r * 0.5f < 38 + r; row++)
            for (var col = -1; col * r * 2 < t.W + 2 * r; col++)
            {
                var cx = t.X + 4 + col * r * 2 + (row % 2 == 1 ? r : 0);
                var cy = top + row * r * 0.5f + r;
                for (var k = 3; k > 0; k--)
                {
                    var radius = r * k / 3;
                    t.Canvas.FillColor = Paper;
                    t.Canvas.FillCircle(cx, cy, radius);
                    t.Canvas.StrokeColor = line;
                    t.Canvas.StrokeSize = 0.9f;
                    t.Canvas.DrawCircle(cx, cy, radius);
                }
            }

        var x = t.Right(22);
        const float y = 26;
        t.Canvas.FillColor = C("#C8402F");
        t.Canvas.FillCircle(x + 5, t.Y + y - 9, 5);
        var mountain = new PathF();
        mountain.MoveTo(x - 14, t.Y + y + 10).LineTo(x - 4, t.Y + y - 4)
            .CurveTo(x - 2, t.Y + y - 6, x + 2, t.Y + y - 6, x + 4, t.Y + y - 4)
            .LineTo(x + 14, t.Y + y + 10).Close();
        t.Fill(C("#1F2F55"), mountain);
        var snow = new PathF();
        snow.MoveTo(x - 4, t.Y + y - 4).CurveTo(x - 2, t.Y + y - 6, x + 2, t.Y + y - 6, x + 4, t.Y + y - 4)
            .LineTo(x + 7, t.Y + y).LineTo(x + 4, t.Y + y - 1).LineTo(x + 2, t.Y + y + 1).LineTo(x, t.Y + y - 1)
            .LineTo(x - 2, t.Y + y + 1).LineTo(x - 4, t.Y + y - 1).LineTo(x - 7, t.Y + y).Close();
        t.Fill(Paper, snow);
    }

    // Three stars in each top corner and red stripes along the bottom edge
    static void America(Tile t)
    {
        var navy = C("#1E3A6B");
        for (var i = 0; i < 3; i++)
        {
            t.Fill(navy, Star(new PointF(t.X + 18 + i * 10, t.Y + 16), 4, 5, 0.42f));
            t.Fill(navy, Star(new PointF(t.Right(18 + i * 10), t.Y + 16), 4, 5, 0.42f));
        }
        t.Canvas.FillColor = C("#B22234");
        for (var i = 0; i < 3; i++)
            t.Canvas.FillRectangle(t.X, t.Bottom(11 - i * 3.4f), t.W, 1.7f);
    }

    // Maple leaves in the top corners and a thin second frame
    static void Canada(Tile t)
    {
        var red = C("#C8202E");
        t.Fill(red, MapleLeaf(new PointF(t.X + 20, t.Y + 20), 22));
        t.Fill(red, MapleLeaf(new PointF(t.Right(20), t.Y + 20), 22));
        t.Canvas.StrokeColor = red.WithAlpha(0.5f);
        t.Canvas.StrokeSize = 0.8f;
        t.Canvas.DrawRoundedRectangle(t.X + 10, t.Y + 10, t.W - 20, t.H - 20, 12);
    }

    // A Paris street plaque: screws in the corners and rules at the top and bottom (the band draws its own inner line)
    static void France(Tile t)
    {
        var green = C("#3E6B4E");
        var rules = new PathF();
        rules.MoveTo(t.X + 16, t.Y + 8).LineTo(t.Right(16), t.Y + 8);
        rules.MoveTo(t.X + 16, t.Bottom(8)).LineTo(t.Right(16), t.Bottom(8));
        t.Stroke(green, 0.8f, rules);
        t.Canvas.FillColor = green;
        foreach (var (x, y) in new[] { (t.X + 14, t.Y + 14), (t.Right(14), t.Y + 14), (t.X + 14, t.Bottom(14)), (t.Right(14), t.Bottom(14)) })
            t.Canvas.FillCircle(x, y, 2.2f);
    }

    // Bauhaus: a red circle, a yellow square and a black triangle
    static void Germany(Tile t)
    {
        t.Canvas.FillColor = C("#C8102E");
        t.Canvas.FillCircle(t.X + 22, t.Y + 22, 9);
        t.Canvas.FillColor = C("#E2A300");
        t.Canvas.FillRectangle(t.Right(32), t.Y + 13, 18, 18);
        var triangle = new PathF();
        triangle.MoveTo(t.Right(32), t.Bottom(14)).LineTo(t.Right(14), t.Bottom(14)).LineTo(t.Right(23), t.Bottom(30)).Close();
        t.Fill(C("#22252C"), triangle);
    }

    // Laurel sprigs rising on either side of the flag
    static void Italy(Tile t)
    {
        Sprig(t, new PointF(t.X + 16, t.Y + 48), 1);
        Sprig(t, new PointF(t.Right(16), t.Y + 48), -1);
    }

    static void Sprig(Tile t, PointF start, int side)
    {
        var green = C("#2F6B3A");
        var stem = new PathF();
        stem.MoveTo(start).QuadTo(start.X + side * 10, start.Y - 8, start.X + side * 22, start.Y - 30);
        t.Stroke(green, 1.1f, stem);
        t.Canvas.FillColor = green;
        for (var i = 1; i <= 5; i++)
        {
            var k = i / 6f;
            var px = start.X + side * (22 * k * k + 10 * k * (1 - k));
            var py = start.Y - (30 * k * k + 8 * k * (1 - k));
            foreach (var leaf in new[] { -1, 1 })
            {
                t.Canvas.SaveState();
                t.Canvas.Rotate(side * 20 + leaf * 35, px + leaf * 3.2f, py + 1);
                t.Canvas.FillEllipse(px + leaf * 3.2f - 1.8f, py + 1 - 4, 3.6f, 8);
                t.Canvas.RestoreState();
            }
        }
    }

    // A Moorish arch over the flag, in gold with a thin red line inside
    static void Spain(Tile t)
    {
        t.Stroke(C("#D9A520"), 1.6f, Arch(t, 30, 4, 52));
        t.Stroke(C("#AA151B"), 0.8f, Arch(t, 36, 10, 56));
    }

    static PathF Arch(Tile t, float side, float top, float shoulder)
    {
        var path = new PathF();
        path.MoveTo(t.X + side, t.Y + BandTop).LineTo(t.X + side, t.Y + 30)
            .CurveTo(t.X + side, t.Y + top + 2, t.X + shoulder, t.Y + top, t.Mid, t.Y + top)
            .CurveTo(t.Right(shoulder), t.Y + top, t.Right(side), t.Y + top + 2, t.Right(side), t.Y + 30)
            .LineTo(t.Right(side), t.Y + BandTop);
        return path;
    }

    // A quarter of an azulejo tile in each corner
    static void Portugal(Tile t)
    {
        var blue = C("#1E4E9C");
        foreach (var (corner, angle) in new[] { (new PointF(t.X + 4, t.Y + 4), 0), (new PointF(t.Right(4), t.Y + 4), 90), (new PointF(t.Right(4), t.Bottom(4)), 180), (new PointF(t.X + 4, t.Bottom(4)), 270) })
        {
            t.Canvas.SaveState();
            t.Canvas.Translate(corner.X, corner.Y);
            t.Canvas.Rotate(angle);
            var quarter = new PathF();
            quarter.MoveTo(0, 0).LineTo(22, 0).CurveTo(22, 22 * K, 22 * K, 22, 0, 22).Close();
            t.Fill(blue.WithAlpha(0.22f), quarter);
            var ring = new PathF();
            ring.MoveTo(14, 0).CurveTo(14, 14 * K, 14 * K, 14, 0, 14);
            t.Stroke(blue, 1.1f, ring);
            var diagonal = new PathF();
            diagonal.MoveTo(0, 0).LineTo(18, 18);
            t.Stroke(blue, 0.8f, diagonal);
            t.Canvas.FillColor = blue;
            t.Canvas.FillCircle(4.5f, 4.5f, 2.4f);
            t.Canvas.RestoreState();
        }
    }

    // How far a cubic's handles reach to draw a quarter circle
    const float K = 0.5523f;

    // Two tulips, Delft blue and orange
    static void Netherlands(Tile t)
    {
        Tulip(t, new PointF(t.X + 22, t.Y + 24), 24, C("#2B4C9B"));
        Tulip(t, new PointF(t.Right(22), t.Y + 24), 24, C("#E8742A"));
    }

    static void Tulip(Tile t, PointF c, float size, Color colour)
    {
        var s = size / 20;
        PointF P(float x, float y) => new(c.X + x * s, c.Y + y * s);
        var flower = new PathF();
        flower.MoveTo(P(-5, -2)).CurveTo(P(-6, -8), P(-3, -10), P(-3, -10)).LineTo(P(0, -6)).LineTo(P(3, -10))
            .CurveTo(P(3, -10), P(6, -8), P(5, -2)).CurveTo(P(4, 2), P(-4, 2), P(-5, -2)).Close();
        t.Fill(colour, flower);
        t.Canvas.FillColor = colour;
        t.Canvas.FillRectangle(c.X - 0.6f * s, c.Y + 1 * s, 1.2f * s, 9 * s);
        var left = new PathF();
        left.MoveTo(P(0, 8)).CurveTo(P(-3, 4), P(-7, 4), P(-8, 2)).CurveTo(P(-6, 7), P(-2, 9), P(0, 9)).Close();
        t.Fill(colour, left);
        var right = new PathF();
        right.MoveTo(P(0, 7)).CurveTo(P(3, 3), P(6, 3), P(8, 1)).CurveTo(P(6, 6), P(2, 8), P(0, 8)).Close();
        t.Fill(colour, right);
    }

    // A Swiss cross in each corner
    static void Switzerland(Tile t)
    {
        var red = C("#D52B1E");
        foreach (var (x, y) in new[] { (t.X + 18, t.Y + 18), (t.Right(18), t.Y + 18), (t.X + 18, t.Bottom(18)), (t.Right(18), t.Bottom(18)) })
        {
            t.Canvas.FillColor = red;
            t.Canvas.FillRectangle(x - 2, y - 6, 4, 12);
            t.Canvas.FillRectangle(x - 6, y - 2, 12, 4);
        }
    }

    // Klimt's gold squares along the top and bottom
    static void Austria(Tile t)
    {
        var gold = C("#B08D3C");
        var count = (int)((t.W - 20) / 15.4f) + 1;
        var step = (t.W - 26) / (count - 1);
        for (var i = 0; i < count; i++)
        {
            t.Canvas.FillColor = i % 2 == 0 ? gold : gold.WithAlpha(0.5f);
            t.Canvas.FillRectangle(t.X + 10 + i * step, t.Y + 7, 6, 6);
            t.Canvas.FillRectangle(t.X + 10 + i * step, t.Bottom(13), 6, 6);
        }
    }

    // An Art Nouveau curve over the flag, with gold dots at its ends and top
    static void Czechia(Tile t)
    {
        var gold = C("#C59B4B");
        var curve = new PathF();
        curve.MoveTo(t.X + 10, t.Y + BandTop)
            .CurveTo(t.X + 10, t.Y + 24, t.X + 30, t.Y + 10, t.X + 52, t.Y + 14)
            .CurveTo(t.X + 66, t.Y + 16, t.X + 72, t.Y + 6, t.Mid, t.Y + 6)
            .CurveTo(t.Right(72), t.Y + 6, t.Right(66), t.Y + 16, t.Right(52), t.Y + 14)
            .CurveTo(t.Right(30), t.Y + 10, t.Right(10), t.Y + 24, t.Right(10), t.Y + BandTop);
        t.Stroke(gold, 1.4f, curve);
        t.Canvas.FillColor = gold;
        t.Canvas.FillCircle(t.Mid, t.Y + 6, 2.4f);
    }

    // Selbu stars in the top corners and a row of knitting along the bottom edge
    static void Norway(Tile t)
    {
        var red = C("#BA0C2F");
        var navy = C("#00205B");
        SelbuStar(t, new PointF(t.X + 22, t.Y + 22), 26, red);
        SelbuStar(t, new PointF(t.Right(22), t.Y + 22), 26, navy);
        t.Canvas.FillColor = navy;
        for (var i = 0; 8 + i * 8 < t.W - 8; i++)
            t.Canvas.FillRectangle(t.X + 8 + i * 8, t.Bottom(i % 2 == 1 ? 7 : 10), 3, 3);
    }

    static void SelbuStar(Tile t, PointF c, float s, Color colour)
    {
        for (var i = 0; i < 8; i++)
        {
            t.Canvas.SaveState();
            t.Canvas.Rotate(i * 45, c.X, c.Y);
            var petal = new PathF();
            petal.MoveTo(c).LineTo(c.X + s * 0.18f, c.Y - s * 0.18f).LineTo(c.X + s * 0.14f, c.Y - s * 0.48f).LineTo(c.X, c.Y - s * 0.38f).Close();
            t.Fill(colour, petal);
            t.Canvas.RestoreState();
        }
    }

    // Cross-stitch motifs on either side of the flag, red and black
    static void Ukraine(Tile t)
    {
        foreach (var x in new[] { t.X + 9, t.X + 25, t.X + 41, t.Right(55), t.Right(39), t.Right(23) })
        {
            foreach (var (col, row) in new[] { (1, 0), (0, 1), (1, 1), (2, 1), (1, 2), (0, 3), (2, 3), (1, 4) })
            {
                var px = x + col * 2.6f;
                var py = t.Y + 10 + row * 2.6f;
                var stitch = new PathF();
                stitch.MoveTo(px, py).LineTo(px + 2.2f, py + 2.2f).MoveTo(px, py + 2.2f).LineTo(px + 2.2f, py);
                t.Stroke(row % 2 == 1 ? C("#1F1F1F") : C("#B5262B"), 1, stitch);
            }
        }
    }

    // Songket teeth along the bottom edge and a hibiscus in the top right
    static void Malaysia(Tile t)
    {
        var gold = C("#C9962B");
        for (var x = t.X + 10; x < t.Right(10); x += 12.7f)
        {
            var outline = new PathF();
            outline.MoveTo(x, t.Bottom(3)).LineTo(x + 6.3f, t.Bottom(11)).LineTo(x + 12.6f, t.Bottom(3));
            t.Stroke(gold, 0.9f, outline);
            var tooth = new PathF();
            tooth.MoveTo(x + 3.2f, t.Bottom(3)).LineTo(x + 6.3f, t.Bottom(7)).LineTo(x + 9.4f, t.Bottom(3)).Close();
            t.Fill(gold, tooth);
        }

        var c = new PointF(t.Right(20), t.Y + 20);
        t.Canvas.FillColor = C("#C8202E").WithAlpha(0.85f);
        for (var i = 0; i < 5; i++)
        {
            t.Canvas.SaveState();
            t.Canvas.Rotate(i * 72, c.X, c.Y);
            t.Canvas.FillEllipse(c.X - 3.6f, c.Y - 5 - 5.2f, 7.2f, 10.4f);
            t.Canvas.RestoreState();
        }
        t.Canvas.FillColor = C("#F2C14E");
        t.Canvas.FillCircle(c.X, c.Y, 1.6f);
    }

    static PathF Star(PointF c, float radius, int points, float inner)
    {
        var path = new PathF();
        for (var i = 0; i < points * 2; i++)
        {
            var angle = -MathF.PI / 2 + i * MathF.PI / points;
            var r = i % 2 == 0 ? radius : radius * inner;
            var p = new PointF(c.X + r * MathF.Cos(angle), c.Y + r * MathF.Sin(angle));
            if (i == 0) path.MoveTo(p); else path.LineTo(p);
        }
        path.Close();
        return path;
    }

    static PathF MapleLeaf(PointF c, float size)
    {
        float[] points =
        [
            0, -10, 2, -6, 4, -7, 3, -2, 7, -5, 8, -3, 10, -4, 9, 0, 11, 1, 6, 5, 7, 7, 1, 6, 1, 10,
            -1, 10, -1, 6, -7, 7, -6, 5, -11, 1, -9, 0, -10, -4, -8, -3, -7, -5, -3, -2, -4, -7, -2, -6,
        ];
        var s = size / 20;
        var path = new PathF();
        for (var i = 0; i < points.Length; i += 2)
        {
            var p = new PointF(c.X + points[i] * s, c.Y + points[i + 1] * s);
            if (i == 0) path.MoveTo(p); else path.LineTo(p);
        }
        path.Close();
        return path;
    }
}
