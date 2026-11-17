using FlagsRally.Helpers;
using FlagsRally.Resources;

namespace FlagsRally.Controls;

/// <summary>
/// The map's one-thumb controller. At rest it is a small round button. Put a thumb on it and the pad
/// opens round the thumb, shaped like a ring doughnut: two rings round a hole. On the inner ring it
/// zooms, in for a slide clockwise from where the thumb came onto the ring and out for one
/// anticlockwise, faster the further round; ＋ and − show on those two sides once the thumb is there.
/// On the outer ring the map keeps moving the way the thumb leans, faster the further out. Lift the
/// thumb and the pad closes to the button again. The pad is see-through so the map shows under it.
/// Only the button takes touches, so the map round it can still be dragged. While the thumb is down
/// the pad reports, about
/// 30 times a second, how far the map should move and how much the zoom changed (<see cref="Moved"/>);
/// the page turns that into camera moves with <see cref="MapPad"/>.
/// </summary>
public class MapThumbPad : Grid
{
    public sealed class MoveEventArgs(double panX, double panY, double zoomChange) : EventArgs
    {
        /// <summary>How far the map's view moves right and down on screen since the last report.</summary>
        public double PanX { get; } = panX;
        public double PanY { get; } = panY;
        /// <summary>Zoom levels since the last report; positive is in.</summary>
        public double ZoomChange { get; } = zoomChange;
    }

    public event EventHandler? Started;
    public event EventHandler<MoveEventArgs>? Moved;

    /// <summary>The button's size, the same as the map's other round buttons.</summary>
    const float ButtonSize = 48;
    // Room round the open pad for its outline.
    const float EdgeRoom = 2;
    const float KnobRadius = 12;
    const float PadOpacity = 0.6f;
    static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(33);

    static readonly Color Navy = AppColors.Get("Navy", "#182A52");
    static readonly Color PaperCard = AppColors.Get("PaperCard", "#FBF8F0");

    static readonly Color PaperEdge = AppColors.Get("PaperEdge", "#DDD2BA");
    static readonly Color NavyDeep = AppColors.Get("NavyDeep", "#0F1C3A");

    readonly IDispatcherTimer _timer;
    readonly GraphicsView _button;   // takes the touches
    readonly GraphicsView _pad;      // drawn round the button while a thumb is down
    PointF _finger;              // from the pad's centre
    DateTime _lastTick;
    bool _touching;
    MapPad.Zone? _zone;
    double _angle;
    double _cameOn;              // the angle at which the finger came onto the inner ring
    double _slide;               // how far round the ring it has slid from there; clockwise is positive

    public MapThumbPad()
    {
        WidthRequest = ButtonSize;
        HeightRequest = ButtonSize;

        // Larger than the button it is centred on, and drawn over what is next to it.
        var padSize = (MapPad.Radius + EdgeRoom) * 2;
        _pad = new GraphicsView
        {
            WidthRequest = padSize,
            HeightRequest = padSize,
            Margin = new Thickness((ButtonSize - padSize) / 2),
            BackgroundColor = Colors.Transparent,
            InputTransparent = true,
            IsVisible = false,
            Drawable = new PadDrawable(this),
        };
        _button = new GraphicsView
        {
            // Room for the shadow under the button.
            WidthRequest = ButtonSize + ButtonShadowRoom * 2,
            HeightRequest = ButtonSize + ButtonShadowRoom * 2,
            Margin = new Thickness(-ButtonShadowRoom),
            BackgroundColor = Colors.Transparent,
            Drawable = new ButtonDrawable(this),
        };
        SemanticProperties.SetDescription(_button, AppResources.MapPad);
        SemanticProperties.SetHint(_button, AppResources.MapPadHint);
        Children.Add(_pad);
        Children.Add(_button);

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = Tick;
        _timer.Tick += (_, _) => Report();

        _button.StartInteraction += (_, e) => OnDown(e.Touches.FirstOrDefault());
        _button.DragInteraction += (_, e) => OnMove(e.Touches.FirstOrDefault());
        _button.EndInteraction += (_, _) => OnUp();
        _button.CancelInteraction += (_, _) => OnUp();
        Unloaded += (_, _) => _timer.Stop();
    }

    const float ButtonShadowRoom = 8;

    PointF FromCentre(PointF touch) => new(touch.X - (float)(_button.Width / 2), touch.Y - (float)(_button.Height / 2));

    void Redraw()
    {
        _pad.IsVisible = _touching;
        _pad.Invalidate();
        _button.Invalidate();
    }

    void OnDown(PointF touch)
    {
        _finger = FromCentre(touch);
        _lastTick = DateTime.UtcNow;
        _touching = true;
        _slide = 0;
        _zone = MapPad.ZoneAt(_finger.Distance(PointF.Zero), null);
        _angle = MapPad.Angle(_finger.X, _finger.Y);
        Started?.Invoke(this, EventArgs.Empty);
        _timer.Start();
        Redraw();
    }

    void OnMove(PointF touch)
    {
        if (!_touching) return;
        _finger = FromCentre(touch);

        var zone = MapPad.ZoneAt(_finger.Distance(PointF.Zero), _zone);
        var angle = MapPad.Angle(_finger.X, _finger.Y);
        if (zone == MapPad.Zone.Zoom)
        {
            if (_zone == MapPad.Zone.Zoom) _slide = MapPad.Slide(_slide, _angle, angle);
            else (_cameOn, _slide) = (angle, 0);
        }
        _zone = zone;
        _angle = angle;
        Redraw();
    }

    void OnUp()
    {
        if (!_touching) return;
        _timer.Stop();
        Report();
        _touching = false;
        _zone = null;
        _slide = 0;
        _finger = PointF.Zero;
        Redraw();
    }

    void Report()
    {
        var now = DateTime.UtcNow;
        var seconds = Math.Min((now - _lastTick).TotalSeconds, 0.1);
        _lastTick = now;

        var (vx, vy) = _zone == MapPad.Zone.Pan ? MapPad.PanVelocity(_finger.X, _finger.Y) : (0, 0);
        var zoomChange = _zone == MapPad.Zone.Zoom ? MapPad.ZoomSpeed(_slide) * seconds : 0;
        if (vx == 0 && vy == 0 && zoomChange == 0) return;
        Moved?.Invoke(this, new MoveEventArgs(vx * seconds, vy * seconds, zoomChange));
    }

    // The button at rest: a paper disc like the my-location button, with the pad's knob and the four
    // ways it moves the map.
    sealed class ButtonDrawable(MapThumbPad pad) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (pad._touching) return;
            var centre = dirtyRect.Center;
            var radius = ButtonSize / 2;

            canvas.SaveState();
            canvas.SetShadow(new SizeF(0, 2), 6, NavyDeep.WithAlpha(0.18f));
            canvas.FillColor = PaperCard;
            canvas.FillCircle(centre, radius);
            canvas.RestoreState();
            canvas.StrokeColor = PaperEdge;
            canvas.StrokeSize = 1;
            canvas.DrawCircle(centre, radius - 0.5f);

            canvas.FillColor = Navy;
            canvas.FillCircle(centre, 6);
            canvas.StrokeColor = Navy;
            canvas.StrokeSize = 2;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeLineJoin = LineJoin.Round;
            const float Tip = 17, Back = 13, Half = 4;
            foreach (var (x, y) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                var path = new PathF();
                path.MoveTo(centre.X + x * Back - y * Half, centre.Y + y * Back - x * Half);
                path.LineTo(centre.X + x * Tip, centre.Y + y * Tip);
                path.LineTo(centre.X + x * Back + y * Half, centre.Y + y * Back + x * Half);
                canvas.DrawPath(path);
            }
        }
    }

    sealed class PadDrawable(MapThumbPad pad) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var centre = dirtyRect.Center;
            var radius = (float)MapPad.Radius;
            var hole = (float)MapPad.HoleRadius;
            var split = (float)MapPad.PanStart;
            var zooming = pad._zone == MapPad.Zone.Zoom;
            var moving = pad._zone == MapPad.Zone.Pan;

            // A ring doughnut, see-through so the map shows under it and through its hole.
            // No shadow: it would show through the paper.
            canvas.StrokeColor = PaperCard.WithAlpha(PadOpacity);
            canvas.StrokeSize = radius - hole;
            canvas.DrawCircle(centre, (radius + hole) / 2);

            // The ring in use is a little darker.
            if (moving)
            {
                canvas.StrokeColor = Navy.WithAlpha(0.1f);
                canvas.StrokeSize = radius - split;
                canvas.DrawCircle(centre, (radius + split) / 2);
            }

            canvas.StrokeSize = 1;
            canvas.StrokeColor = Navy.WithAlpha(0.45f);
            canvas.DrawCircle(centre, radius);
            canvas.DrawCircle(centre, hole);
            canvas.StrokeColor = Navy.WithAlpha(0.25f);
            canvas.DrawCircle(centre, split);

            if (zooming)
            {
                var inner = (split + hole) / 2;
                var outer = (radius + split) / 2;
                var cameOn = (float)(pad._cameOn * 180 / Math.PI);
                var slide = (float)(pad._slide * 180 / Math.PI);
                var speed = MapPad.ZoomSpeed(pad._slide);
                canvas.StrokeLineCap = LineCap.Butt;

                // How far the thumb has slid from where it came on: the longer the band, the faster the zoom.
                canvas.StrokeColor = Navy.WithAlpha(0.3f);
                canvas.StrokeSize = split - hole - 4;
                DrawArc(canvas, centre, inner, cameOn, cameOn + slide);

                // Where it came on, with ＋ on its clockwise side and − on the other. They are drawn on the
                // outer ring, clear of the thumb, and the one in use is filled in.
                // Drawn as strokes so they look the same on Android and iOS.
                canvas.StrokeLineCap = LineCap.Round;
                canvas.StrokeColor = Navy;
                canvas.StrokeSize = 2;
                canvas.DrawLine(At(centre, hole + 3, cameOn), At(centre, split - 3, cameOn));
                DrawZoomSign(canvas, At(centre, outer, cameOn + Math.Max(slide, 0) + SignAhead), plus: true, inUse: speed > 0);
                DrawZoomSign(canvas, At(centre, outer, cameOn + Math.Min(slide, 0) - SignAhead), plus: false, inUse: speed < 0);
            }

            var (kx, ky) = MapPad.KnobOffset(pad._finger.X, pad._finger.Y, radius - KnobRadius);
            canvas.FillColor = Navy.WithAlpha(0.9f);
            canvas.FillCircle(centre.X + (float)kx, centre.Y + (float)ky, KnobRadius);
        }

        static void DrawZoomSign(ICanvas canvas, PointF at, bool plus, bool inUse)
        {
            if (inUse)
            {
                canvas.FillColor = Navy;
                canvas.FillCircle(at, 11);
            }
            canvas.StrokeColor = inUse ? PaperCard : Navy;
            canvas.StrokeSize = 2;
            DrawSign(canvas, at.X, at.Y, 6, plus);
        }

        static void DrawSign(ICanvas canvas, float x, float y, float half, bool plus)
        {
            canvas.DrawLine(x - half, y, x + half, y);
            if (plus) canvas.DrawLine(x, y - half, x, y + half);
        }

        // ＋ and − stay this far (degrees) ahead of the thumb, so it does not cover them.
        const float SignAhead = 40;

        // A point on a circle round the pad; degrees grow clockwise from the right.
        static PointF At(PointF centre, float radius, double degrees)
        {
            var a = degrees * Math.PI / 180;
            return new PointF(centre.X + radius * (float)Math.Cos(a), centre.Y + radius * (float)Math.Sin(a));
        }

        static void DrawArc(ICanvas canvas, PointF centre, float radius, float fromDegrees, float toDegrees)
        {
            if (Math.Abs(toDegrees - fromDegrees) < 1) return;
            var path = new PathF();
            const int Steps = 24;
            for (var i = 0; i <= Steps; i++)
            {
                var point = At(centre, radius, fromDegrees + (toDegrees - fromDegrees) * i / Steps);
                if (i == 0) path.MoveTo(point); else path.LineTo(point);
            }
            canvas.DrawPath(path);
        }
    }
}
