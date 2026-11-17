using FlagsRally.Helpers;
using FlagsRally.Resources;

namespace FlagsRally.Controls;

/// <summary>
/// The map's one-thumb controller. Lean the thumb inside the inner circle and the map keeps moving
/// that way; slide it out to the ring and round the pad to zoom (clockwise in), without lifting.
/// A short tap on the ring's ＋ (right) or − (left) zooms one step. The pad is see-through so the map
/// shows under it, and the arrows on the ring and the sign in the middle say which way zooms in. While the thumb is down the pad reports, about
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
    const double TurnShown = 0.08;   // zoom levels one way before the middle shows ＋ or −
    const float PadOpacity = 0.5f;
    const float PadOpacityInUse = 0.62f;
    static readonly TimeSpan TapTime = TimeSpan.FromMilliseconds(300);
    static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(33);

    static readonly Color Navy = AppColors.Get("Navy", "#182A52");
    static readonly Color PaperCard = AppColors.Get("PaperCard", "#FBF8F0");

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
    double _turned;              // zoom levels since the turn last changed direction
    int _turning;                // +1 while zooming in, −1 out, 0 before the ring has been turned

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
        _turned = 0;
        _turning = 0;
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
            var change = MapPad.ZoomChange(_angle, angle);
            _zoomChange += change;
            NoteTurn(change);
        }
        else if (zone != MapPad.Zone.Zoom)
        {
            _turning = 0;
            _turned = 0;
        }
        _zone = zone;
        _angle = angle;
        Invalidate();
    }

    // The sign in the middle follows the turn, but not a finger's wobble.
    void NoteTurn(double change)
    {
        if (change == 0) return;
        if (Math.Sign(change) != Math.Sign(_turned)) _turned = 0;
        _turned += change;
        if (Math.Abs(_turned) >= TurnShown) _turning = Math.Sign(_turned);
    }

    void OnUp(bool canceled)
    {
        if (!_touching) return;
        _timer.Stop();
        Report();
        var tapped = !canceled && !_moved && DateTime.UtcNow - _downAt < TapTime;
        _touching = false;
        _zone = null;
        _turning = 0;
        _finger = PointF.Zero;
        Invalidate();

        var step = tapped ? MapPad.StepAt(_down.X, _down.Y) : 0;
        if (step != 0) Stepped?.Invoke(this, step);
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

            // See-through, so the map shows under the pad. No shadow: it would show through the paper.
            canvas.FillColor = PaperCard.WithAlpha(pad._touching ? PadOpacityInUse : PadOpacity);
            canvas.FillCircle(centre, radius);

            // The ring lights up while it is zooming.
            if (zooming)
            {
                canvas.StrokeColor = Navy.WithAlpha(0.16f);
                canvas.StrokeSize = radius - ring;
                canvas.DrawCircle(centre, (radius + ring) / 2);
            }

            canvas.StrokeColor = Navy.WithAlpha(0.45f);
            canvas.StrokeSize = 1;
            canvas.DrawCircle(centre, radius);
            canvas.StrokeColor = Navy.WithAlpha(0.25f);
            canvas.DrawCircle(centre, ring);

            // Like a dial: ＋ on the right with an arrow turning clockwise towards it, − on the left
            // with one turning the other way. Drawn as strokes so they look the same on Android and iOS.
            var mark = (radius + ring) / 2;
            canvas.StrokeColor = Navy;
            canvas.StrokeSize = 2;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeLineJoin = LineJoin.Round;
            DrawSign(canvas, centre.X + mark, centre.Y, 6, plus: true);
            DrawSign(canvas, centre.X - mark, centre.Y, 6, plus: false);
            canvas.StrokeSize = 1.5f;
            DrawTurnArrow(canvas, centre, mark, -72, -24);
            DrawTurnArrow(canvas, centre, mark, -108, -156);

            // While the ring is turned, the middle says what the turn is doing.
            if (zooming && pad._turning != 0)
            {
                canvas.StrokeSize = 3;
                DrawSign(canvas, centre.X, centre.Y, 11, plus: pad._turning > 0);
            }

            var (kx, ky) = MapPad.KnobOffset(pad._finger.X, pad._finger.Y, radius - KnobRadius);
            canvas.FillColor = Navy.WithAlpha(pad._touching ? 0.9f : 0.8f);
            canvas.FillCircle(centre.X + (float)kx, centre.Y + (float)ky, KnobRadius);
        }

        static void DrawSign(ICanvas canvas, float x, float y, float half, bool plus)
        {
            canvas.DrawLine(x - half, y, x + half, y);
            if (plus) canvas.DrawLine(x, y - half, x, y + half);
        }

        // An arc round the pad from one angle to another (degrees, growing clockwise from the right),
        // with an arrowhead at its end.
        static void DrawTurnArrow(ICanvas canvas, PointF centre, float radius, float fromDegrees, float toDegrees)
        {
            PointF At(double degrees)
            {
                var a = degrees * Math.PI / 180;
                return new PointF(centre.X + radius * (float)Math.Cos(a), centre.Y + radius * (float)Math.Sin(a));
            }

            var path = new PathF();
            const int Steps = 12;
            for (var i = 0; i <= Steps; i++)
            {
                var point = At(fromDegrees + (toDegrees - fromDegrees) * i / Steps);
                if (i == 0) path.MoveTo(point); else path.LineTo(point);
            }
            canvas.DrawPath(path);

            // The head's two strokes lean back from the way the arc is going.
            var tip = At(toDegrees);
            var going = (toDegrees + (toDegrees > fromDegrees ? 90 : -90)) * Math.PI / 180;
            const float Head = 5.5f;
            foreach (var lean in new[] { 2.5, -2.5 })
            {
                var a = going + lean;
                canvas.DrawLine(tip.X, tip.Y, tip.X + Head * (float)Math.Cos(a), tip.Y + Head * (float)Math.Sin(a));
            }
        }
    }
}
