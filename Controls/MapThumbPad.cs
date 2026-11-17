using FlagsRally.Helpers;
using FlagsRally.Resources;

namespace FlagsRally.Controls;

/// <summary>
/// The map's one-thumb controller. At rest it is a small round button. Put a thumb on it and the pad
/// opens round the thumb: lean inside the inner circle and the map keeps moving that way; slide out
/// to the ring and it zooms, in for a slide clockwise from where the thumb came onto the ring and out
/// for one anticlockwise, faster the further round. The ring shows ＋ and − on those two sides of the
/// thumb once it is there. Lift the thumb and the pad closes to the button again. The pad is
/// see-through so the map shows under it. Only the button takes touches, so the map round it can
/// still be dragged. While the thumb is down the pad reports, about
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
    const float KnobRadius = 15;
    const double MoveSlop = 8;
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
    PointF _down;
    DateTime _lastTick;
    bool _touching;
    bool _moved;
    MapPad.Zone? _zone;
    double _angle;
    double _cameOn;              // the angle at which the finger came onto the ring
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
        _finger = _down = FromCentre(touch);
        _lastTick = DateTime.UtcNow;
        _touching = true;
        _moved = false;
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
        if (_finger.Distance(_down) > MoveSlop) _moved = true;

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

        var (vx, vy) = _zone == MapPad.Zone.Pan && _moved ? MapPad.PanVelocity(_finger.X, _finger.Y) : (0, 0);
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
            var ring = (float)MapPad.RingStart;
            var zooming = pad._zone == MapPad.Zone.Zoom;

            // See-through, so the map shows under the pad. No shadow: it would show through the paper.
            canvas.FillColor = PaperCard.WithAlpha(PadOpacity);
            canvas.FillCircle(centre, radius);

            canvas.StrokeColor = Navy.WithAlpha(0.45f);
            canvas.StrokeSize = 1;
            canvas.DrawCircle(centre, radius);
            canvas.StrokeColor = Navy.WithAlpha(0.25f);
            canvas.DrawCircle(centre, ring);

            if (zooming)
            {
                var mark = (radius + ring) / 2;
                var cameOn = (float)(pad._cameOn * 180 / Math.PI);
                var slide = (float)(pad._slide * 180 / Math.PI);
                var speed = MapPad.ZoomSpeed(pad._slide);
                canvas.StrokeLineCap = LineCap.Round;
                canvas.StrokeLineJoin = LineJoin.Round;

                // How far the thumb has slid from where it came on: the longer the band, the faster the zoom.
                canvas.StrokeColor = Navy.WithAlpha(0.3f);
                canvas.StrokeSize = radius - ring - 8;
                DrawArc(canvas, centre, mark, cameOn, cameOn + slide);

                // Where it came on, with ＋ on its clockwise side and − on the other, clear of the thumb.
                // Drawn as strokes so they look the same on Android and iOS.
                canvas.StrokeColor = Navy;
                canvas.StrokeSize = 2;
                var start = At(centre, ring + 3, cameOn);
                var end = At(centre, radius - 3, cameOn);
                canvas.DrawLine(start, end);
                var plus = At(centre, mark, cameOn + Math.Max(slide, 0) + SignAhead);
                var minus = At(centre, mark, cameOn + Math.Min(slide, 0) - SignAhead);
                DrawSign(canvas, plus.X, plus.Y, 6, plus: true);
                DrawSign(canvas, minus.X, minus.Y, 6, plus: false);

                // The middle says what the zoom is doing now.
                if (speed != 0)
                {
                    canvas.StrokeSize = 3;
                    DrawSign(canvas, centre.X, centre.Y, 11, plus: speed > 0);
                }
            }

            var (kx, ky) = MapPad.KnobOffset(pad._finger.X, pad._finger.Y, radius - KnobRadius);
            canvas.FillColor = Navy.WithAlpha(0.9f);
            canvas.FillCircle(centre.X + (float)kx, centre.Y + (float)ky, KnobRadius);
        }

        static void DrawSign(ICanvas canvas, float x, float y, float half, bool plus)
        {
            canvas.DrawLine(x - half, y, x + half, y);
            if (plus) canvas.DrawLine(x, y - half, x, y + half);
        }

        // ＋ and − stay this far (degrees) ahead of the thumb, so it does not cover them.
        const float SignAhead = 42;

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
