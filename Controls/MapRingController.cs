using FlagsRally.Helpers;
using FlagsRally.Resources;

namespace FlagsRally.Controls;

/// <summary>
/// The map's ring controller, worked with one thumb at the map's bottom right (see <see cref="MapRing"/>
/// for the sums).
/// <para>
/// Closed, only its button shows (<see cref="MapRingButton"/>, in the column of the map's round
/// buttons; see <see cref="Button"/>). A tap on it opens the controller, which grows out of the button
/// to its own place: a ring round the outside, with 36 ticks that turn with the zoom, and a base in the
/// middle with a knob on it. The page closes it again when the map outside is touched
/// (<see cref="Close"/>), and the controller shrinks back into the button; the map keeps its zoom and
/// place. It does not close while a thumb is on it.
/// </para>
/// <list type="bullet">
/// <item>A touch that starts on the base works the knob, a joystick: the map moves the way it leans
/// until the thumb lifts, when the knob springs back. A double tap there goes back to the start
/// (<see cref="ResetRequested"/>).</item>
/// <item>A touch that starts on the ring is a shuttle: the point it came down on is marked, and the
/// angle the thumb has slid round from it zooms in (clockwise) or out (anticlockwise), also while the
/// thumb is held still.</item>
/// </list>
/// The ring shows how it is used: a clockwise arrow to ＋ and an anticlockwise one to − at its top,
/// where the thumb does not cover them. The page can have it demonstrate the slide when it opens
/// (<see cref="PlayDemo"/>), and is told when the ring is only tapped (<see cref="RingTapped"/>) and
/// when it zooms (<see cref="RingZoomed"/>), to explain it or to stop explaining it.
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
    /// <summary>The ring was tapped without sliding round it.</summary>
    public event EventHandler? RingTapped;
    /// <summary>The ring zoomed the map, once for each touch that does.</summary>
    public event EventHandler? RingZoomed;

    public State CurrentState { get; private set; }

    /// <summary>The zoom's speed in levels per second while the ring is worked; positive is in.</summary>
    public double ZoomSpeed { get; private set; }

    public bool IsOpen { get; private set; }

    const float Outer = (float)MapRing.OuterRadius;
    const float Base = (float)MapRing.BaseRadius;
    const float Knob = (float)MapRing.KnobRadius;
    const float ButtonSize = MapRingButton.Size;
    const uint OpenMs = 180;
    // The demonstration: a thumb slides this far round clockwise from the top and back, the ticks
    // turning a little with it, in this long.
    static readonly TimeSpan DemoTime = TimeSpan.FromMilliseconds(1400);
    const double DemoSlideDegrees = 50;
    const double DemoTickDegrees = 20;
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
    static readonly Color Paper = AppColors.Get("Paper", "#F4EEDF");
    static readonly Color PaperCard = AppColors.Get("PaperCard", "#FBF8F0");
    static readonly Color PaperEdge = AppColors.Get("PaperEdge", "#DDD2BA");
    static readonly Color InkMuted = AppColors.Get("InkMuted", "#7A7262");

    readonly IDispatcherTimer _timer;
    readonly GraphicsView _ring;     // the open controller; takes its own touches
    MapRingButton? _button;          // the closed controller
    SizeF _buttonOffset;             // the button's centre from the ring's, where it grows from and shrinks to

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
    DateTime? _demoStart;            // when the demonstration starts, while one is to play
    bool _zoomedThisTouch;

    public MapRingController()
    {
        var size = Outer * 2;
        WidthRequest = size;
        HeightRequest = size;
        // Only the open ring takes touches: the map round it can still be dragged.
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
        Children.Add(_ring);

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = Tick;
        _timer.Tick += (_, _) => OnTick();

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

    /// <summary>The button that stands for the closed controller and opens it when tapped.</summary>
    public MapRingButton? Button
    {
        get => _button;
        set
        {
            if (_button is not null) _button.Tapped -= OnButtonTapped;
            _button = value;
            if (_button is not null) _button.Tapped += OnButtonTapped;
        }
    }

    void OnButtonTapped(object? sender, EventArgs e) => Open();

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        OpenChanged?.Invoke(this, true);

        _ring.CancelAnimations();
        _buttonOffset = SizeF.Zero;
        if (_button is not null)
        {
            _button.CancelAnimations();
            _buttonOffset = CentreOnPage(_button) - CentreOnPage(this);
            SetButtonShown(false);
        }

        // From the button's place and size to the controller's.
        _ring.TranslationX = _buttonOffset.Width;
        _ring.TranslationY = _buttonOffset.Height;
        _ring.Scale = ButtonSize / (Outer * 2);
        _ring.Opacity = 0;
        _ring.IsVisible = true;
        _ring.Invalidate();
        _ = _ring.TranslateToAsync(0, 0, OpenMs, Easing.CubicOut);
        _ = _ring.ScaleToAsync(1, OpenMs, Easing.CubicOut);
        _ = _ring.FadeToAsync(1, OpenMs / 2);
    }

    /// <summary>
    /// Shows how the ring is used, once the controller has opened: a thumb slides round it clockwise
    /// and back, and ＋ lights up. The map does not move. A touch stops it.
    /// </summary>
    public void PlayDemo()
    {
        if (!IsOpen) return;
        _demoStart = DateTime.UtcNow + TimeSpan.FromMilliseconds(OpenMs);
        _timer.Start();
    }

    // 0 to 1 through the demonstration, or null when none is playing.
    double? DemoPart()
    {
        if (_demoStart is not { } start) return null;
        var part = (DateTime.UtcNow - start) / DemoTime;
        if (part < 0) return 0;
        if (part >= 1)
        {
            _demoStart = null;
            return null;
        }
        return part;
    }

    // The button keeps its room in the column while the controller is open, so the buttons round it stay put.
    void SetButtonShown(bool shown)
    {
        if (_button is null) return;
        _button.Opacity = shown ? 1 : 0;
        _button.InputTransparent = !shown;
    }

    // Where a view's centre is on the page, with the moves its parents were given (the map's buttons
    // are lifted above the panels with a translation).
    static PointF CentreOnPage(VisualElement view)
    {
        double x = view.Width / 2, y = view.Height / 2;
        for (Element? element = view; element is VisualElement visual and not Page; element = element.Parent)
        {
            x += visual.X + visual.TranslationX;
            y += visual.Y + visual.TranslationY;
        }
        return new PointF((float)x, (float)y);
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
        _demoStart = null;
        OpenChanged?.Invoke(this, false);

        _ring.CancelAnimations();
        _button?.CancelAnimations();
        _ = ShrinkAsync();
    }

    async Task ShrinkAsync()
    {
        // Back to the button's place and size.
        var move = _ring.TranslateToAsync(_buttonOffset.Width, _buttonOffset.Height, CloseMs, Easing.CubicIn);
        var scale = _ring.ScaleToAsync(ButtonSize / (Outer * 2), CloseMs, Easing.CubicIn);
        var fade = _ring.FadeToAsync(0, CloseMs, Easing.CubicIn);
        // They report true when they were cut short: the controller was opened again meanwhile.
        var canceled = await move | await scale | await fade;
        if (canceled) return;
        _ring.IsVisible = false;
        if (_button is null) return;
        SetButtonShown(true);
        _button.Opacity = 0;
        await _button.FadeToAsync(1, 100);
    }

    void OnDown(PointF at)
    {
        var part = MapRing.PartAt(at.X, at.Y);
        _part = part;
        _demoStart = null;
        _zoomedThisTouch = false;
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

        if (part == MapRing.Part.Ring && tapped) RingTapped?.Invoke(this, EventArgs.Empty);
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

        if (_part is null && _knob == PointF.Zero && _moveGlow == 0 && _zoomGlow == 0 && _demoStart is null) _timer.Stop();
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
            if (speed != 0)
            {
                Moved?.Invoke(this, new MoveEventArgs(0, 0, speed * seconds));
                if (!_zoomedThisTouch)
                {
                    _zoomedThisTouch = true;
                    RingZoomed?.Invoke(this, EventArgs.Empty);
                }
            }
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
    internal static void DrawMoveMark(ICanvas canvas, PointF centre, float size, Color ink, float strokeSize)
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

    // The open controller. Everything is drawn round one centre, the ticks included.
    sealed class RingDrawable(MapRingController c) : IDrawable
    {
        const float TickOuter = Outer - 5;
        // The ring's radius while the knob is worked: a narrow band round the base.
        const float ShrunkOuter = Base + 8;
        const float TickLength = 6;
        const float LongTickLength = 10;
        // The shuttle's arc runs a little inside the ticks.
        const float ArcRadius = TickOuter - LongTickLength - 9;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var centre = new PointF(Outer, Outer);
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeLineJoin = LineJoin.Round;

            // The ring: a paper face with a thin edge. While the knob is worked it shrinks to a narrow
            // band round the base, out of the way of the map, and its ticks fade.
            var outer = Outer - (Outer - ShrunkOuter) * c._moveGlow;
            canvas.FillColor = PaperCard.WithAlpha(0.92f);
            canvas.FillCircle(centre, outer - 1);
            canvas.StrokeColor = InkMuted.WithAlpha(0.5f);
            canvas.StrokeSize = 1;
            canvas.DrawCircle(centre, outer - 1);

            // The demonstration: the thumb slides round clockwise and back, the ticks turning with it.
            var demo = c.DemoPart();
            var demoSlide = demo is { } part ? (float)Math.Sin(Math.Min(1, part / 0.85) * Math.PI) : 0;

            // 36 ticks, every 10°, longer every 90°, turned with the zoom.
            if (c._moveGlow < 1)
            {
                var shrink = outer - Outer;
                canvas.StrokeColor = InkMuted.WithAlpha(1 - c._moveGlow);
                canvas.StrokeSize = 1.4f;
                for (var i = 0; i < 36; i++)
                {
                    var angle = i * 10 + c._tickRotation + demoSlide * DemoTickDegrees;
                    var length = i % 9 == 0 ? LongTickLength : TickLength;
                    canvas.DrawLine(OnCircle(centre, TickOuter + shrink, angle), OnCircle(centre, TickOuter + shrink - length, angle));
                }
            }

            // How the ring is used, always there: at the top, out of the thumb's way, a clockwise arrow
            // to ＋ on the right and an anticlockwise one to − on the left. Fainter while it is worked.
            var signAlpha = 0.6f * (1 - Math.Max(c._moveGlow, c._zoomGlow * 0.75f));
            if (signAlpha > 0.01f)
            {
                DrawSign(canvas, centre, -80, -48, plus: true, demoSlide > 0.3f ? Math.Min(1, signAlpha + 0.4f) : signAlpha);
                DrawSign(canvas, centre, -100, -132, plus: false, signAlpha);
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

            // The demonstration's thumb, see-through, on the ring.
            if (demo is { } shown)
            {
                var fade = (float)(shown < 0.1 ? shown / 0.1 : shown > 0.85 ? (1 - shown) / 0.15 : 1);
                var thumb = OnCircle(centre, ArcRadius, -90 + demoSlide * DemoSlideDegrees);
                canvas.FillColor = ZoomColor.WithAlpha(0.45f * fade);
                canvas.FillCircle(thumb, 11);
                canvas.StrokeColor = PaperCard.WithAlpha(0.9f * fade);
                canvas.StrokeSize = 2;
                canvas.DrawCircle(thumb, 11);
            }

            // The knob, in the move colour while it is worked.
            var knob = new PointF(centre.X + c._knob.X, centre.Y + c._knob.Y);
            canvas.FillColor = Mix(PaperCard, MoveColor, c._moveGlow);
            canvas.FillCircle(knob, Knob);
            canvas.StrokeColor = Mix(PaperEdge, MoveColor, c._moveGlow);
            canvas.StrokeSize = 1;
            canvas.DrawCircle(knob, Knob);
            DrawMoveMark(canvas, knob, 12, Mix(InkMuted, PaperCard, c._moveGlow), 1.8f);
        }

        // An arrow round the ring from one angle to another, with ＋ or − just past its head.
        static void DrawSign(ICanvas canvas, PointF centre, double from, double to, bool plus, float alpha)
        {
            var lit = alpha > 0.61f;
            var ink = ZoomColor.WithAlpha(alpha);
            var way = Math.Sign(to - from);
            var arc = new PathF();
            for (var i = 0; i <= 12; i++)
            {
                var point = OnCircle(centre, ArcRadius, from + (to - from) * i / 12);
                if (i == 0) arc.MoveTo(point); else arc.LineTo(point);
            }
            canvas.StrokeColor = ink;
            canvas.StrokeSize = 2;
            canvas.DrawPath(arc);

            // The head points along the circle at the arrow's end.
            var head = OnCircle(centre, ArcRadius, to);
            var along = (to + way * 90) * Math.PI / 180;
            var outwards = to * Math.PI / 180;
            float ax = (float)Math.Cos(along), ay = (float)Math.Sin(along);
            float ox = (float)Math.Cos(outwards), oy = (float)Math.Sin(outwards);
            var tip = new PathF();
            tip.MoveTo(head.X + ax * 5, head.Y + ay * 5);
            tip.LineTo(head.X - ax * 2 + ox * 4, head.Y - ay * 2 + oy * 4);
            tip.LineTo(head.X - ax * 2 - ox * 4, head.Y - ay * 2 - oy * 4);
            tip.Close();
            canvas.FillColor = ink;
            canvas.FillPath(tip);

            var sign = OnCircle(centre, ArcRadius, to + way * 14);
            var half = lit ? 5f : 4f;
            canvas.StrokeSize = lit ? 2.4f : 2;
            canvas.DrawLine(sign.X - half, sign.Y, sign.X + half, sign.Y);
            if (plus) canvas.DrawLine(sign.X, sign.Y - half, sign.X, sign.Y + half);
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
