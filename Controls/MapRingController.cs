using FlagsRally.Helpers;
using FlagsRally.Resources;

namespace FlagsRally.Controls;

/// <summary>
/// The map's ring controller, worked with one thumb at the map's bottom right (see <see cref="MapRing"/>
/// for the sums).
/// <para>
/// Closed, it is a small round button with the four-way move mark, at the controller's centre. A tap
/// opens the controller, which grows out of the button: a ring round the outside, with 36 ticks that
/// turn with the zoom, and a base in the middle with a knob on it. The page closes it again when the
/// map outside is touched (<see cref="Close"/>), and the controller shrinks back into the button; the
/// map keeps its zoom and place. It does not close while a thumb is on it.
/// </para>
/// <list type="bullet">
/// <item>A touch that starts on the base works the knob, a joystick: the map moves the way it leans
/// until the thumb lifts, when the knob springs back. A double tap there goes back to the start
/// (<see cref="ResetRequested"/>).</item>
/// <item>A touch that starts on the ring is a shuttle: the point it came down on is marked, and the
/// angle the thumb has slid round from it zooms in (clockwise) or out (anticlockwise), also while the
/// thumb is held still.</item>
/// </list>
/// What a touch does is chosen where it starts and kept if the thumb crosses the base's edge. The base
/// is ringed in the move colour while the knob is worked, the ring in the zoom colour while it is.
/// The controller reports about 60 times a second how far the map should move and zoom
/// (<see cref="Moved"/>); the page turns that into camera moves.
/// </summary>
public class MapRingController : Grid
{
    public sealed class MoveEventArgs(double panX, double panY, double zoomChange) : EventArgs
    {
        /// <summary>How far the map's view moves right and down on screen since the last report.</summary>
        public double PanX { get; } = panX;
        public double PanY { get; } = panY;
        /// <summary>Zoom levels since the last report; positive is in.</summary>
        public double ZoomChange { get; } = zoomChange;
    }

    public enum State { Idle, Moving, Zooming }

    /// <summary>A thumb has come down; what it does next starts from the camera as it is now.</summary>
    public event EventHandler? Started;
    public event EventHandler<MoveEventArgs>? Moved;
    /// <summary>The middle was double-tapped: back to the starting zoom and place.</summary>
    public event EventHandler? ResetRequested;
    /// <summary>The controller opened (true) or closed (false).</summary>
    public event EventHandler<bool>? OpenChanged;
    /// <summary><see cref="CurrentState"/> or <see cref="ZoomSpeed"/> changed.</summary>
    public event EventHandler? StateChanged;

    public State CurrentState { get; private set; }

    /// <summary>The zoom's speed in levels per second while the ring is worked; positive is in.</summary>
    public double ZoomSpeed { get; private set; }

    public bool IsOpen { get; private set; }

    const float Outer = (float)MapRing.OuterRadius;
    const float Base = (float)MapRing.BaseRadius;
    const float Knob = (float)MapRing.KnobRadius;
    const float ButtonSize = 48;
    const float ShadowRoom = 8;
    const uint OpenMs = 180;
    const uint CloseMs = 150;
    // How long the colours take to change, and the knob to spring back.
    const double GlowSeconds = 0.15;
    const double KnobReturnSeconds = 0.05;

    // A touch this short that moved this little is a tap; two on the middle this close together, a double tap.
    static readonly TimeSpan TapTime = TimeSpan.FromMilliseconds(300);
    static readonly TimeSpan DoubleTapTime = TimeSpan.FromMilliseconds(350);
    const float TapSlop = 10;

    static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(16);

    static readonly Color MoveColor = AppColors.Get("Navy", "#182A52");
    static readonly Color ZoomColor = AppColors.Get("Stamp", "#B94047");
    static readonly Color NavyDeep = AppColors.Get("NavyDeep", "#0F1C3A");
    static readonly Color Paper = AppColors.Get("Paper", "#F4EEDF");
    static readonly Color PaperCard = AppColors.Get("PaperCard", "#FBF8F0");
    static readonly Color PaperEdge = AppColors.Get("PaperEdge", "#DDD2BA");
    static readonly Color InkMuted = AppColors.Get("InkMuted", "#7A7262");

    readonly IDispatcherTimer _timer;
    readonly GraphicsView _ring;     // the open controller; takes its own touches
    readonly GraphicsView _button;   // the closed controller

    MapRing.Part? _part;             // what the thumb that is down works
    PointF _finger;                  // from the centre
    PointF _down;
    DateTime _downAt;
    DateTime _lastTick;
    DateTime _lastMiddleTap;
    bool _slid;
    PointF _knob;                    // where the knob is drawn, from the centre
    double _referenceAngle;          // where the thumb came down on the ring
    double _lastAngle;
    double _shift;                   // how far round from the reference point
    float _moveGlow, _zoomGlow;      // 0 to 1: how far the colours have changed
    double _tickRotation;

    public MapRingController()
    {
        var size = Outer * 2;
        WidthRequest = size;
        HeightRequest = size;
        // Only the button, or the open ring, takes touches: the map round them can still be dragged.
        InputTransparent = true;
        CascadeInputTransparent = false;

        _ring = new GraphicsView
        {
            WidthRequest = size,
            HeightRequest = size,
            BackgroundColor = Colors.Transparent,
            IsVisible = false,
            Drawable = new RingDrawable(this),
        };
        _button = new GraphicsView
        {
            WidthRequest = ButtonSize + ShadowRoom * 2,
            HeightRequest = ButtonSize + ShadowRoom * 2,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            BackgroundColor = Colors.Transparent,
            Drawable = new ButtonDrawable(),
        };
        SemanticProperties.SetDescription(_button, AppResources.MapRing);
        SemanticProperties.SetHint(_button, AppResources.MapRingHint);
        Children.Add(_ring);
        Children.Add(_button);

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = Tick;
        _timer.Tick += (_, _) => OnTick();

        _button.EndInteraction += (_, e) =>
        {
            if (e.IsInsideBounds) Open();
        };
        _ring.StartInteraction += (_, e) => OnDown(FromCentre(e.Touches.FirstOrDefault()));
        _ring.DragInteraction += (_, e) => OnMove(FromCentre(e.Touches.FirstOrDefault()));
        _ring.EndInteraction += (_, _) => OnUp(canceled: false);
        _ring.CancelInteraction += (_, _) => OnUp(canceled: true);
        Unloaded += (_, _) => _timer.Stop();
    }

    PointF FromCentre(PointF touch) => new(touch.X - Outer, touch.Y - Outer);

    /// <summary>The map's zoom now, which turns the ring's ticks.</summary>
    public void ShowZoom(double zoom, double homeZoom)
    {
        var rotation = MapRing.TickRotation(zoom, homeZoom);
        if (rotation == _tickRotation) return;
        _tickRotation = rotation;
        if (IsOpen) _ring.Invalidate();
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        OpenChanged?.Invoke(this, true);

        _button.CancelAnimations();
        _ring.CancelAnimations();
        _button.IsVisible = false;
        _ring.Scale = ButtonSize / (Outer * 2);
        _ring.Opacity = 0;
        _ring.IsVisible = true;
        _ring.Invalidate();
        _ = _ring.ScaleToAsync(1, OpenMs, Easing.CubicOut);
        _ = _ring.FadeToAsync(1, OpenMs / 2);
    }

    /// <summary>
    /// Back to the button, unless a thumb is working the controller (or <paramref name="evenWhileHeld"/>,
    /// for leaving the page). The map keeps its zoom and place.
    /// </summary>
    public void Close(bool evenWhileHeld = false)
    {
        if (!IsOpen) return;
        if (_part is not null)
        {
            if (!evenWhileHeld) return;
            EndTouch();
        }
        IsOpen = false;
        OpenChanged?.Invoke(this, false);

        _ring.CancelAnimations();
        _button.CancelAnimations();
        _ = ShrinkAsync();
    }

    async Task ShrinkAsync()
    {
        var scale = _ring.ScaleToAsync(ButtonSize / (Outer * 2), CloseMs, Easing.CubicIn);
        var fade = _ring.FadeToAsync(0, CloseMs, Easing.CubicIn);
        // Both report true when they were cut short: the controller was opened again meanwhile.
        var canceled = await scale | await fade;
        if (canceled) return;
        _ring.IsVisible = false;
        _button.Opacity = 0;
        _button.IsVisible = true;
        await _button.FadeToAsync(1, 100);
    }

    void OnDown(PointF at)
    {
        var part = MapRing.PartAt(at.X, at.Y);
        _part = part;
        _finger = _down = at;
        _downAt = _lastTick = DateTime.UtcNow;
        _slid = false;
        Started?.Invoke(this, EventArgs.Empty);

        if (part == MapRing.Part.Outside)
        {
            // The square's corner, outside the ring: the map under it is touched, so the controller
            // closes, and the drag moves the map as it would have.
            _part = null;
            Close();
            _part = MapRing.Part.Outside;
            return;
        }

        if (part == MapRing.Part.Ring)
        {
            _referenceAngle = _lastAngle = MapRing.AngleOf(at.X, at.Y);
            _shift = 0;
        }
        SetState(part == MapRing.Part.Ring ? State.Zooming : State.Moving, 0);
        _timer.Start();
        _ring.Invalidate();
    }

    void OnMove(PointF at)
    {
        if (_part is null) return;
        var previous = _finger;
        _finger = at;
        if (_finger.Distance(_down) > TapSlop) _slid = true;

        switch (_part)
        {
            case MapRing.Part.Outside:
                // Dragging the map: it goes with the finger, so the view moves the other way.
                Moved?.Invoke(this, new MoveEventArgs(previous.X - at.X, previous.Y - at.Y, 0));
                return;
            case MapRing.Part.Ring:
                var angle = MapRing.AngleOf(at.X, at.Y);
                _shift = MapRing.AddStep(_shift, _lastAngle, angle);
                _lastAngle = angle;
                break;
        }
        _ring.Invalidate();
    }

    void OnUp(bool canceled)
    {
        if (_part is null) return;
        var part = _part;
        var tapped = !canceled && !_slid && DateTime.UtcNow - _downAt < TapTime;
        Report();
        EndTouch();

        if (part == MapRing.Part.Joystick && tapped)
        {
            var now = DateTime.UtcNow;
            if (now - _lastMiddleTap < DoubleTapTime)
            {
                _lastMiddleTap = default;
                ResetRequested?.Invoke(this, EventArgs.Empty);
            }
            else _lastMiddleTap = now;
        }
    }

    // The thumb is off: the knob springs back and the colours fade, run by the timer.
    void EndTouch()
    {
        _part = null;
        _shift = 0;
        SetState(State.Idle, 0);
        _timer.Start();
        _ring.Invalidate();
    }

    void SetState(State state, double zoomSpeed)
    {
        if (state == CurrentState && zoomSpeed == ZoomSpeed) return;
        CurrentState = state;
        ZoomSpeed = zoomSpeed;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    void OnTick()
    {
        var now = DateTime.UtcNow;
        var seconds = (float)Math.Min((now - _lastTick).TotalSeconds, 0.1);
        if (_part is not null && _part != MapRing.Part.Outside) Report();
        else _lastTick = now;

        // The knob follows the thumb inside the base, or springs back to the middle.
        var limit = Base - Knob;
        if (_part == MapRing.Part.Joystick)
        {
            var (x, y) = MapRing.KnobOffset(_finger.X, _finger.Y, limit);
            _knob = new PointF((float)x, (float)y);
        }
        else
        {
            var keep = (float)Math.Exp(-seconds / KnobReturnSeconds);
            _knob = new PointF(_knob.X * keep, _knob.Y * keep);
            if (_knob.Distance(PointF.Zero) < 0.3f) _knob = PointF.Zero;
        }

        var step = (float)(seconds / GlowSeconds);
        _moveGlow = Approach(_moveGlow, CurrentState == State.Moving ? 1 : 0, step);
        _zoomGlow = Approach(_zoomGlow, CurrentState == State.Zooming ? 1 : 0, step);
        _ring.Invalidate();

        if (_part is null && _knob == PointF.Zero && _moveGlow == 0 && _zoomGlow == 0) _timer.Stop();
    }

    static float Approach(float value, float target, float step) =>
        value < target ? Math.Min(target, value + step) : Math.Max(target, value - step);

    void Report()
    {
        var now = DateTime.UtcNow;
        var seconds = Math.Min((now - _lastTick).TotalSeconds, 0.1);
        _lastTick = now;

        if (_part == MapRing.Part.Joystick)
        {
            var (vx, vy) = MapRing.PanVelocity(_finger.X, _finger.Y);
            if (vx != 0 || vy != 0) Moved?.Invoke(this, new MoveEventArgs(vx * seconds, vy * seconds, 0));
        }
        else if (_part == MapRing.Part.Ring)
        {
            var speed = MapRing.ZoomSpeed(_shift);
            SetState(State.Zooming, speed);
            if (speed != 0) Moved?.Invoke(this, new MoveEventArgs(0, 0, speed * seconds));
        }
    }

    static Color Mix(Color from, Color to, float amount) => new(
        from.Red + (to.Red - from.Red) * amount,
        from.Green + (to.Green - from.Green) * amount,
        from.Blue + (to.Blue - from.Blue) * amount,
        from.Alpha + (to.Alpha - from.Alpha) * amount);

    static PointF OnCircle(PointF centre, float radius, double degrees)
    {
        var angle = degrees * Math.PI / 180;
        return new PointF(centre.X + radius * (float)Math.Cos(angle), centre.Y + radius * (float)Math.Sin(angle));
    }

    // The four-way move mark: a cross with an arrowhead at each end.
    static void DrawMoveMark(ICanvas canvas, PointF centre, float size, Color ink, float strokeSize)
    {
        canvas.StrokeColor = ink;
        canvas.StrokeSize = strokeSize;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.DrawLine(centre.X - size, centre.Y, centre.X + size, centre.Y);
        canvas.DrawLine(centre.X, centre.Y - size, centre.X, centre.Y + size);
        var head = size * 0.38f;
        foreach (var (x, y) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
        {
            var tip = new PointF(centre.X + x * size, centre.Y + y * size);
            var path = new PathF();
            path.MoveTo(tip.X - x * head - y * head, tip.Y - y * head - x * head);
            path.LineTo(tip);
            path.LineTo(tip.X - x * head + y * head, tip.Y - y * head + x * head);
            canvas.DrawPath(path);
        }
    }

    // The closed controller: a paper disc like the map's other round buttons, with the move mark.
    sealed class ButtonDrawable : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
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
            DrawMoveMark(canvas, centre, 11, MoveColor, 1.8f);
        }
    }

    // The open controller. Everything is drawn round one centre, the ticks included.
    sealed class RingDrawable(MapRingController c) : IDrawable
    {
        const float TickOuter = Outer - 5;
        const float TickLength = 6;
        const float LongTickLength = 10;
        // The shuttle's arc runs a little inside the ticks.
        const float ArcRadius = TickOuter - LongTickLength - 9;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var centre = new PointF(Outer, Outer);
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeLineJoin = LineJoin.Round;

            // The ring: a paper face with a thin edge.
            canvas.FillColor = PaperCard.WithAlpha(0.92f);
            canvas.FillCircle(centre, Outer - 1);
            canvas.StrokeColor = InkMuted.WithAlpha(0.5f);
            canvas.StrokeSize = 1;
            canvas.DrawCircle(centre, Outer - 1);

            // 36 ticks, every 10°, longer every 90°, turned with the zoom.
            canvas.StrokeColor = InkMuted;
            canvas.StrokeSize = 1.4f;
            for (var i = 0; i < 36; i++)
            {
                var angle = i * 10 + c._tickRotation;
                var length = i % 9 == 0 ? LongTickLength : TickLength;
                canvas.DrawLine(OnCircle(centre, TickOuter, angle), OnCircle(centre, TickOuter - length, angle));
            }

            // Zooming: the inside of the ring's edge in the zoom colour.
            if (c._zoomGlow > 0)
            {
                canvas.StrokeColor = ZoomColor.WithAlpha(c._zoomGlow);
                canvas.StrokeSize = 3;
                canvas.DrawCircle(centre, Outer - 2.5f);
            }

            // The base: a darker paper face with a thin edge; its edge is where the ring starts.
            canvas.FillColor = Paper;
            canvas.FillCircle(centre, Base);
            canvas.StrokeColor = InkMuted.WithAlpha(0.5f);
            canvas.StrokeSize = 1;
            canvas.DrawCircle(centre, Base);

            // Moving: the inside of the base's edge in the move colour.
            if (c._moveGlow > 0)
            {
                canvas.StrokeColor = MoveColor.WithAlpha(c._moveGlow);
                canvas.StrokeSize = 3;
                canvas.DrawCircle(centre, Base - 1.5f);
            }

            if (c._part == MapRing.Part.Ring) DrawShuttle(canvas, centre);

            // The knob, in the move colour while it is worked.
            var knob = new PointF(centre.X + c._knob.X, centre.Y + c._knob.Y);
            canvas.FillColor = Mix(PaperCard, MoveColor, c._moveGlow);
            canvas.FillCircle(knob, Knob);
            canvas.StrokeColor = Mix(PaperEdge, MoveColor, c._moveGlow);
            canvas.StrokeSize = 1;
            canvas.DrawCircle(knob, Knob);
            DrawMoveMark(canvas, knob, 12, Mix(InkMuted, PaperCard, c._moveGlow), 1.8f);
        }

        // The reference point, the thumb and the arc between them, round the way the thumb went.
        void DrawShuttle(ICanvas canvas, PointF centre)
        {
            var from = c._referenceAngle;
            var to = c._referenceAngle + c._shift;
            if (Math.Abs(c._shift) > 0.5)
            {
                var arc = new PathF();
                var steps = Math.Max(2, (int)(Math.Abs(c._shift) / 3));
                for (var i = 0; i <= steps; i++)
                {
                    var point = OnCircle(centre, ArcRadius, from + (to - from) * i / steps);
                    if (i == 0) arc.MoveTo(point); else arc.LineTo(point);
                }
                canvas.StrokeColor = ZoomColor.WithAlpha(0.35f);
                canvas.StrokeSize = 9;
                canvas.DrawPath(arc);
            }

            var reference = OnCircle(centre, ArcRadius, from);
            canvas.FillColor = PaperCard;
            canvas.FillCircle(reference, 5);
            canvas.StrokeColor = ZoomColor;
            canvas.StrokeSize = 2;
            canvas.DrawCircle(reference, 5);

            canvas.FillColor = ZoomColor;
            canvas.FillCircle(OnCircle(centre, ArcRadius, to), 7.5f);
        }
    }
}
