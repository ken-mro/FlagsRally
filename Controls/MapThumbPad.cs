using FlagsRally.Helpers;
using FlagsRally.Resources;

namespace FlagsRally.Controls;

/// <summary>
/// The map's one-thumb controller. Lean the thumb inside the inner circle and the map keeps moving
/// that way; slide it out to the ring and round the pad to zoom (clockwise in), without lifting.
/// A short tap on the ring's ＋ or − zooms one step. While the thumb is down the pad reports, about
/// 30 times a second, how far the map should move and how much the zoom changed (<see cref="Moved"/>);
/// the page turns that into camera moves with <see cref="MapPad"/>.
/// </summary>
public class MapThumbPad : GraphicsView
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

    /// <summary>+1 for a tap on ＋ (zoom in), −1 for a tap on −.</summary>
    public event EventHandler<int>? Stepped;

    // Room round the pad for its shadow.
    const float ShadowRoom = 6;
    const float KnobRadius = 15;
    const double TapSlop = 8;
    static readonly TimeSpan TapTime = TimeSpan.FromMilliseconds(300);
    static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(33);

    static readonly Color Navy = AppColors.Get("Navy", "#182A52");
    static readonly Color NavyDeep = AppColors.Get("NavyDeep", "#0F1C3A");
    static readonly Color PaperCard = AppColors.Get("PaperCard", "#FBF8F0");
    static readonly Color PaperEdge = AppColors.Get("PaperEdge", "#DDD2BA");

    readonly IDispatcherTimer _timer;
    PointF _finger;              // from the pad's centre
    PointF _down;
    DateTime _downAt;
    DateTime _lastTick;
    bool _touching;
    bool _moved;
    MapPad.Zone? _zone;
    double _angle;
    double _zoomChange;          // gathered between reports

    public MapThumbPad()
    {
        var size = (MapPad.Radius + ShadowRoom) * 2;
        WidthRequest = size;
        HeightRequest = size;
        BackgroundColor = Colors.Transparent;
        Drawable = new PadDrawable(this);
        SemanticProperties.SetDescription(this, AppResources.MapPad);
        SemanticProperties.SetHint(this, AppResources.MapPadHint);

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = Tick;
        _timer.Tick += (_, _) => Report();

        StartInteraction += (_, e) => OnDown(e.Touches.FirstOrDefault());
        DragInteraction += (_, e) => OnMove(e.Touches.FirstOrDefault());
        EndInteraction += (_, _) => OnUp(canceled: false);
        CancelInteraction += (_, _) => OnUp(canceled: true);
        Unloaded += (_, _) => _timer.Stop();
    }

    PointF FromCentre(PointF touch) => new(touch.X - (float)(Width / 2), touch.Y - (float)(Height / 2));

    void OnDown(PointF touch)
    {
        _finger = _down = FromCentre(touch);
        _downAt = _lastTick = DateTime.UtcNow;
        _touching = true;
        _moved = false;
        _zoomChange = 0;
        _zone = MapPad.ZoneAt(_finger.Distance(PointF.Zero), null);
        _angle = MapPad.Angle(_finger.X, _finger.Y);
        Started?.Invoke(this, EventArgs.Empty);
        _timer.Start();
        Invalidate();
    }

    void OnMove(PointF touch)
    {
        if (!_touching) return;
        _finger = FromCentre(touch);
        if (_finger.Distance(_down) > TapSlop) _moved = true;

        var zone = MapPad.ZoneAt(_finger.Distance(PointF.Zero), _zone);
        var angle = MapPad.Angle(_finger.X, _finger.Y);
        // Only a slide along the ring zooms; the move that entered it does not.
        if (zone == MapPad.Zone.Zoom && _zone == MapPad.Zone.Zoom)
        {
            _zoomChange += MapPad.ZoomChange(_angle, angle);
        }
        _zone = zone;
        _angle = angle;
        Invalidate();
    }

    void OnUp(bool canceled)
    {
        if (!_touching) return;
        _timer.Stop();
        Report();
        var tapped = !canceled && !_moved && DateTime.UtcNow - _downAt < TapTime;
        var wasOnRing = _zone == MapPad.Zone.Zoom;
        _touching = false;
        _zone = null;
        _finger = PointF.Zero;
        Invalidate();

        // The ring's upper part is ＋, its lower part −.
        if (tapped && wasOnRing && Math.Abs(_down.Y) > Math.Abs(_down.X))
        {
            Stepped?.Invoke(this, _down.Y < 0 ? 1 : -1);
        }
    }

    void Report()
    {
        var now = DateTime.UtcNow;
        var seconds = Math.Min((now - _lastTick).TotalSeconds, 0.1);
        _lastTick = now;

        var (vx, vy) = _zone == MapPad.Zone.Pan && _moved ? MapPad.PanVelocity(_finger.X, _finger.Y) : (0, 0);
        var zoomChange = _zoomChange;
        _zoomChange = 0;
        if (vx == 0 && vy == 0 && zoomChange == 0) return;
        Moved?.Invoke(this, new MoveEventArgs(vx * seconds, vy * seconds, zoomChange));
    }

    sealed class PadDrawable(MapThumbPad pad) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var centre = dirtyRect.Center;
            var radius = (float)MapPad.Radius;
            var ring = (float)MapPad.RingStart;
            var zooming = pad._zone == MapPad.Zone.Zoom;

            canvas.SaveState();
            canvas.SetShadow(new SizeF(0, 2), 6, NavyDeep.WithAlpha(0.18f));
            canvas.FillColor = PaperCard;
            canvas.FillCircle(centre, radius);
            canvas.RestoreState();

            // The ring lights up while it is zooming.
            if (zooming)
            {
                canvas.StrokeColor = Navy.WithAlpha(0.14f);
                canvas.StrokeSize = radius - ring;
                canvas.DrawCircle(centre, (radius + ring) / 2);
            }

            canvas.StrokeColor = PaperEdge;
            canvas.StrokeSize = 1;
            canvas.DrawCircle(centre, radius);
            canvas.DrawCircle(centre, ring);

            // ＋ and − on the ring, drawn as strokes so they look the same on Android and iOS.
            var mark = (radius + ring) / 2;
            canvas.StrokeColor = Navy;
            canvas.StrokeSize = 2;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.DrawLine(centre.X - 6, centre.Y - mark, centre.X + 6, centre.Y - mark);
            canvas.DrawLine(centre.X, centre.Y - mark - 6, centre.X, centre.Y - mark + 6);
            canvas.DrawLine(centre.X - 6, centre.Y + mark, centre.X + 6, centre.Y + mark);

            var (kx, ky) = MapPad.KnobOffset(pad._finger.X, pad._finger.Y, radius - KnobRadius);
            canvas.FillColor = Navy;
            canvas.FillCircle(centre.X + (float)kx, centre.Y + (float)ky, KnobRadius);
        }
    }
}
