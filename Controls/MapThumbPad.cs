using FlagsRally.Helpers;
using FlagsRally.Resources;

namespace FlagsRally.Controls;

/// <summary>
/// The map's thumb controller. At rest it is a small round button above the my-location button. It
/// opens a pad to move the map and one to zoom it, each with its middle at a mark: one to the left
/// of the button, one above, far enough from the my-location button that a pad there does not cover it.
/// The move pad is a ring, on which the map keeps moving the way the thumb leans, faster the further
/// out, and not at all in the hole in its middle. The zoom pad is an upright bar with ＋ at the top and
/// − at the bottom, where above the middle zooms in and below zooms out, faster the further away.
/// <para>
/// There are two ways to use it. Slide: put a thumb on the button, slide to a mark, and the pad opens
/// with its middle under the thumb; lift the thumb and it closes. Tap: tap the button and the marks
/// stay; tap a mark and its pad stays open at the mark, to be pressed and dragged as often as wanted;
/// a tap anywhere else puts the button back. The page passes on the touches that land outside the
/// button while something is kept open (<see cref="TouchDown"/>).
/// </para>
/// The pads are see-through so the map shows under them. While nothing is kept open only the button
/// takes touches, so the map round it can still be dragged. While a thumb is on a pad the controller
/// reports, about 30 times a second, how far the map should move and how much the zoom changed
/// (<see cref="Moved"/>); the page turns that into camera moves with <see cref="MapPad"/>.
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

    /// <summary>A thumb has come down; what it does next starts from the camera as it is now.</summary>
    public event EventHandler? Started;
    public event EventHandler<MoveEventArgs>? Moved;

    /// <summary>
    /// The marks or a pad are now kept open after a tap (true), or no longer (false). While they are,
    /// the page passes on the touches outside this view's button (<see cref="TouchDown"/>): the marks
    /// and the pads are drawn outside the button's bounds, where a view gets no touches of its own.
    /// </summary>
    public event EventHandler<bool>? KeptOpenChanged;

    /// <summary>The button's size, the same as the map's other round buttons.</summary>
    const float ButtonSize = 48;
    const float ButtonShadowRoom = 8;
    const float KnobRadius = 14;
    const float PadOpacity = 0.6f;
    // How far the drawing reaches from the button's centre.
    const float Reach = 170;

    // The marks stand where the pads open: how large they are, and where, from the button's centre.
    const float MarkRadius = 17;
    static readonly PointF MoveMarkAt = new(-(float)MapPad.MoveSwitch, 0);
    static readonly PointF ZoomMarkAt = new(0, -(float)MapPad.ZoomSwitch);

    // A pad opened by sliding has its middle under the thumb, but no nearer the my-location button
    // below the controller (its centre is 58 under this one's, see LocationPage.xaml) than leaves it
    // clear: the move pad's middle goes no lower than the button's centre, so the ring keeps 2 away...
    const float MoveMiddleMaxY = 0;
    // ...and the zoom pad's goes no further right than this, so the bar stays inside the map's edge.
    const float ZoomMiddleMaxX = 4;

    // A touch this short that moved this little is a tap.
    static readonly TimeSpan TapTime = TimeSpan.FromMilliseconds(350);
    const float TapSlop = 10;
    // How far round a mark or a pad a touch still counts as on it.
    const float MarkSlop = 9;
    const float PadSlop = 6;

    static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(33);

    static readonly Color Navy = AppColors.Get("Navy", "#182A52");
    static readonly Color NavyDeep = AppColors.Get("NavyDeep", "#0F1C3A");
    static readonly Color PaperCard = AppColors.Get("PaperCard", "#FBF8F0");
    static readonly Color PaperEdge = AppColors.Get("PaperEdge", "#DDD2BA");

    readonly IDispatcherTimer _timer;
    readonly GraphicsView _button;   // takes the touches at rest
    readonly GraphicsView _pad;      // the marks and the pads; drawn only, it takes no touches
    PointF _finger;                  // from the button's centre
    PointF _middle;                  // the open pad's middle, from the button's centre
    PointF _down;                    // where this touch came down
    DateTime _downAt;
    MapPad.Mode? _mode;              // the open pad; null while only the marks show
    DateTime _lastTick;
    bool _touching;                  // a thumb is down
    bool _keptOpen;                  // the marks or a pad stay after the thumb lifts (opened by a tap)
    bool _slid;                      // this touch has moved too far to be a tap

    public MapThumbPad()
    {
        WidthRequest = ButtonSize;
        HeightRequest = ButtonSize;

        // Larger than the button it is centred on, and drawn over what is next to it.
        _pad = new GraphicsView
        {
            WidthRequest = Reach * 2,
            HeightRequest = Reach * 2,
            Margin = new Thickness(ButtonSize / 2 - Reach),
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

        _button.StartInteraction += (_, e) => OnDown(FromCentre(e.Touches.FirstOrDefault()));
        _button.DragInteraction += (_, e) => OnMove(FromCentre(e.Touches.FirstOrDefault()));
        _button.EndInteraction += (_, _) => OnUp(canceled: false);
        _button.CancelInteraction += (_, _) => OnUp(canceled: true);
        Unloaded += (_, _) => _timer.Stop();
    }

    PointF FromCentre(PointF touch) => new(touch.X - (float)(_button.Width / 2), touch.Y - (float)(_button.Height / 2));

    /// <summary>
    /// A touch outside the button, passed on by the page while the marks or a pad are kept open.
    /// The point is measured from this view's centre, which is the button's.
    /// </summary>
    public void TouchDown(PointF fromCentre) => OnDown(fromCentre);

    public void TouchMove(PointF fromCentre) => OnMove(fromCentre);

    public void TouchUp(bool canceled) => OnUp(canceled);

    bool Showing => _touching || _keptOpen;

    void Redraw()
    {
        _pad.IsVisible = Showing;
        _pad.Invalidate();
        _button.Invalidate();
    }

    void SetKeptOpen(bool keptOpen)
    {
        if (_keptOpen == keptOpen) return;
        _keptOpen = keptOpen;
        KeptOpenChanged?.Invoke(this, keptOpen);
    }

    /// <summary>Back to the button: the marks or the pad that a tap left open go.</summary>
    public void Close()
    {
        _timer.Stop();
        _touching = false;
        _mode = null;
        _finger = _middle = PointF.Zero;
        SetKeptOpen(false);
        Redraw();
    }

    void OpenPad(MapPad.Mode mode, PointF middle)
    {
        _mode = mode;
        _middle = middle;
    }

    void OnDown(PointF at)
    {
        if (_keptOpen)
        {
            if (_mode is { } pad)
            {
                // A pad is kept open: a touch on it works it, one beside it closes it.
                if (!MapPad.IsOnPad(pad, at.X - _middle.X, at.Y - _middle.Y, PadSlop))
                {
                    Close();
                    return;
                }
            }
            else if (at.Distance(MoveMarkAt) <= MarkRadius + MarkSlop) OpenPad(MapPad.Mode.Move, MoveMarkAt);
            else if (at.Distance(ZoomMarkAt) <= MarkRadius + MarkSlop) OpenPad(MapPad.Mode.Zoom, ZoomMarkAt);
            else if (at.Distance(PointF.Zero) > ButtonSize / 2 + MarkSlop)
            {
                // Beside the marks and the button.
                Close();
                return;
            }
        }

        _finger = _down = at;
        _downAt = _lastTick = DateTime.UtcNow;
        _touching = true;
        _slid = false;
        Started?.Invoke(this, EventArgs.Empty);
        _timer.Start();
        Redraw();
    }

    void OnMove(PointF at)
    {
        if (!_touching) return;
        _finger = at;
        if (_finger.Distance(_down) > TapSlop) _slid = true;
        if (_mode is null)
        {
            // Sliding from the button: the pad opens with its middle under the thumb.
            var mode = MapPad.ModeAt(_finger.X, _finger.Y);
            if (mode == MapPad.Mode.Move) OpenPad(mode.Value, new PointF(_finger.X, Math.Min(_finger.Y, MoveMiddleMaxY)));
            else if (mode == MapPad.Mode.Zoom) OpenPad(mode.Value, new PointF(Math.Min(_finger.X, ZoomMiddleMaxX), _finger.Y));
            // A pad opened by sliding closes when the thumb lifts, also if the marks were kept open.
            if (mode is not null) SetKeptOpen(false);
        }
        Redraw();
    }

    void OnUp(bool canceled)
    {
        if (!_touching) return;
        _timer.Stop();
        Report();
        _touching = false;
        var tapped = !canceled && !_slid && DateTime.UtcNow - _downAt < TapTime;

        if (_mode is not null && _keptOpen)
        {
            // A pad kept open stays, with its knob back in the middle.
            _finger = _middle;
            Redraw();
            return;
        }
        if (_mode is null && tapped)
        {
            if (_keptOpen)
            {
                // A second tap on the button puts the marks away.
                Close();
                return;
            }
            // A tap on the button: the marks stay, to be tapped.
            _finger = PointF.Zero;
            SetKeptOpen(true);
            Redraw();
            return;
        }
        Close();
    }

    void Report()
    {
        var now = DateTime.UtcNow;
        var seconds = Math.Min((now - _lastTick).TotalSeconds, 0.1);
        _lastTick = now;

        var x = _finger.X - _middle.X;
        var y = _finger.Y - _middle.Y;
        if (_mode == MapPad.Mode.Zoom)
        {
            var speed = MapPad.ZoomSpeed(y);
            if (speed != 0) Moved?.Invoke(this, new MoveEventArgs(0, 0, speed * seconds));
        }
        else if (_mode == MapPad.Mode.Move)
        {
            var (vx, vy) = MapPad.PanVelocity(x, y);
            if (vx != 0 || vy != 0) Moved?.Invoke(this, new MoveEventArgs(vx * seconds, vy * seconds, 0));
        }
    }

    static void DrawSign(ICanvas canvas, float x, float y, float half, bool plus)
    {
        canvas.DrawLine(x - half, y, x + half, y);
        if (plus) canvas.DrawLine(x, y - half, x, y + half);
    }

    // An arrowhead pointing along (dx, dy), which is one of the four ways.
    static void DrawChevron(ICanvas canvas, PointF centre, int dx, int dy, float tip, float back, float half)
    {
        var path = new PathF();
        path.MoveTo(centre.X + dx * back - dy * half, centre.Y + dy * back - dx * half);
        path.LineTo(centre.X + dx * tip, centre.Y + dy * tip);
        path.LineTo(centre.X + dx * back + dy * half, centre.Y + dy * back + dx * half);
        canvas.DrawPath(path);
    }

    static readonly (int X, int Y)[] FourWays = [(1, 0), (-1, 0), (0, 1), (0, -1)];

    // The four arrows that stand for moving the map, and ＋ over − for the zoom.
    static void DrawMoveMark(ICanvas canvas, PointF centre, float size, Color ink)
    {
        canvas.FillColor = ink;
        canvas.FillCircle(centre, size * 0.22f);
        foreach (var (x, y) in FourWays) DrawChevron(canvas, centre, x, y, tip: size, back: size * 0.7f, half: size * 0.3f);
    }

    static void DrawZoomMark(ICanvas canvas, PointF centre, float size)
    {
        DrawSign(canvas, centre.X, centre.Y - size * 0.55f, size * 0.42f, plus: true);
        DrawSign(canvas, centre.X, centre.Y + size * 0.6f, size * 0.42f, plus: false);
    }

    // The button: a paper disc like the my-location button, with the move mark at its left and the
    // zoom mark at its top, the ways to slide for each. Navy while a thumb is on it.
    sealed class ButtonDrawable(MapThumbPad pad) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            // The open pad is drawn over where the button is.
            if (pad._mode is not null) return;
            var active = pad.Showing;
            var centre = dirtyRect.Center;
            var radius = ButtonSize / 2;

            canvas.SaveState();
            canvas.SetShadow(new SizeF(0, 2), 6, NavyDeep.WithAlpha(0.18f));
            canvas.FillColor = active ? Navy : PaperCard;
            canvas.FillCircle(centre, radius);
            canvas.RestoreState();
            canvas.StrokeColor = active ? Navy : PaperEdge;
            canvas.StrokeSize = 1;
            canvas.DrawCircle(centre, radius - 0.5f);

            var ink = active ? PaperCard : Navy;
            canvas.StrokeColor = ink;
            canvas.StrokeSize = 1.6f;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeLineJoin = LineJoin.Round;
            DrawMoveMark(canvas, new PointF(centre.X - 9, centre.Y + 6), 8, ink);
            DrawZoomMark(canvas, new PointF(centre.X + 9, centre.Y - 8), 8);
        }
    }

    // What is drawn while a thumb is down. See-through, so the map shows under it; no shadow, which
    // would show through the paper.
    sealed class PadDrawable(MapThumbPad pad) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeLineJoin = LineJoin.Round;
            var button = dirtyRect.Center;
            var finger = new PointF(pad._finger.X - pad._middle.X, pad._finger.Y - pad._middle.Y);
            switch (pad._mode)
            {
                case MapPad.Mode.Move: DrawMove(canvas, new PointF(button.X + pad._middle.X, button.Y + pad._middle.Y), finger); break;
                case MapPad.Mode.Zoom: DrawZoom(canvas, new PointF(button.X + pad._middle.X, button.Y + pad._middle.Y), finger); break;
                default: DrawChoice(canvas, button); break;
            }
        }

        // Before a pad is open: the two places to slide to.
        static void DrawChoice(ICanvas canvas, PointF button)
        {
            var move = new PointF(button.X + MoveMarkAt.X, button.Y + MoveMarkAt.Y);
            var zoom = new PointF(button.X + ZoomMarkAt.X, button.Y + ZoomMarkAt.Y);
            // A row of dots from the button to each place.
            canvas.FillColor = Navy.WithAlpha(0.55f);
            for (var x = button.X - ButtonSize / 2 - 6; x > move.X + MarkRadius + 3; x -= 7) canvas.FillCircle(x, button.Y, 1.5f);
            for (var y = button.Y - ButtonSize / 2 - 6; y > zoom.Y + MarkRadius + 3; y -= 7) canvas.FillCircle(button.X, y, 1.5f);
            foreach (var mark in new[] { move, zoom })
            {
                canvas.FillColor = PaperCard.WithAlpha(0.85f);
                canvas.FillCircle(mark, MarkRadius);
                canvas.StrokeColor = Navy.WithAlpha(0.45f);
                canvas.StrokeSize = 1;
                canvas.DrawCircle(mark, MarkRadius);
            }
            canvas.StrokeColor = Navy;
            canvas.StrokeSize = 2;
            DrawMoveMark(canvas, move, 10, Navy);
            DrawZoomMark(canvas, zoom, 10);
        }

        static void DrawMove(ICanvas canvas, PointF centre, PointF finger)
        {
            var radius = (float)MapPad.Radius;
            var hole = (float)MapPad.PanDeadZone;

            // A ring: the hole in the middle is where nothing moves, and the map shows through it.
            canvas.StrokeColor = PaperCard.WithAlpha(PadOpacity);
            canvas.StrokeSize = radius - hole;
            canvas.StrokeLineCap = LineCap.Butt;
            canvas.DrawCircle(centre, (radius + hole) / 2);
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeColor = Navy.WithAlpha(0.45f);
            canvas.StrokeSize = 1;
            canvas.DrawCircle(centre, radius);
            canvas.DrawCircle(centre, hole);

            var (vx, vy) = MapPad.PanVelocity(finger.X, finger.Y);
            var speed = Math.Sqrt(vx * vx + vy * vy);
            var (kx, ky) = MapPad.KnobOffset(finger.X, finger.Y, radius - KnobRadius);
            var knob = new PointF(centre.X + (float)kx, centre.Y + (float)ky);

            // From the hole's edge to the thumb: the longer the band, the faster the map moves.
            var reach = Math.Sqrt(kx * kx + ky * ky);
            if (reach > hole)
            {
                var from = new PointF(centre.X + (float)(kx / reach * hole), centre.Y + (float)(ky / reach * hole));
                canvas.StrokeColor = Navy.WithAlpha(0.3f);
                canvas.StrokeSize = 12;
                canvas.StrokeLineCap = LineCap.Butt;
                canvas.DrawLine(from, knob);
                canvas.StrokeLineCap = LineCap.Round;
            }

            // An arrowhead at each of the four ways; the ones the map is moving towards are filled in,
            // like the zoom bar's ＋ and −.
            foreach (var (x, y) in FourWays)
            {
                var towards = speed > 0 && (vx * x + vy * y) / speed > 0.38;
                var at = new PointF(centre.X + x * (radius - 15), centre.Y + y * (radius - 15));
                if (towards)
                {
                    canvas.FillColor = Navy;
                    canvas.FillCircle(at, 11);
                }
                canvas.FillColor = towards ? PaperCard : Navy;
                var head = new PathF();
                head.MoveTo(at.X + x * 5.5f, at.Y + y * 5.5f);
                head.LineTo(at.X - x * 3.5f - y * 5.5f, at.Y - y * 3.5f - x * 5.5f);
                head.LineTo(at.X - x * 3.5f + y * 5.5f, at.Y - y * 3.5f + x * 5.5f);
                head.Close();
                canvas.FillPath(head);
            }

            canvas.FillColor = Navy.WithAlpha(0.9f);
            canvas.FillCircle(knob, KnobRadius);
        }

        static void DrawZoom(ICanvas canvas, PointF centre, PointF finger)
        {
            var halfWidth = (float)MapPad.ZoomHalfWidth;
            var halfHeight = (float)MapPad.ZoomHalfHeight;
            var bar = new RectF(centre.X - halfWidth, centre.Y - halfHeight, halfWidth * 2, halfHeight * 2);
            canvas.FillColor = PaperCard.WithAlpha(PadOpacity);
            canvas.FillRoundedRectangle(bar, halfWidth);
            canvas.StrokeColor = Navy.WithAlpha(0.45f);
            canvas.StrokeSize = 1;
            canvas.DrawRoundedRectangle(bar, halfWidth);

            var speed = MapPad.ZoomSpeed(finger.Y);
            var knobY = Math.Clamp(finger.Y, -(halfHeight - KnobRadius), halfHeight - KnobRadius);

            // From the middle to the thumb: the longer the band, the faster the zoom.
            if (Math.Abs(knobY) > 1)
            {
                canvas.StrokeColor = Navy.WithAlpha(0.3f);
                canvas.StrokeSize = 12;
                canvas.DrawLine(centre.X, centre.Y, centre.X, centre.Y + knobY);
            }
            // The middle, where nothing changes.
            canvas.StrokeColor = Navy.WithAlpha(0.6f);
            canvas.StrokeSize = 2;
            canvas.DrawLine(centre.X - 12, centre.Y, centre.X + 12, centre.Y);

            // ＋ at the top and − at the bottom; the one in use is filled in.
            // Drawn as strokes so they look the same on Android and iOS.
            DrawEnd(canvas, new PointF(centre.X, centre.Y - halfHeight + 15), plus: true, inUse: speed > 0);
            DrawEnd(canvas, new PointF(centre.X, centre.Y + halfHeight - 15), plus: false, inUse: speed < 0);

            canvas.FillColor = Navy.WithAlpha(0.9f);
            canvas.FillCircle(centre.X, centre.Y + knobY, KnobRadius);
        }

        static void DrawEnd(ICanvas canvas, PointF at, bool plus, bool inUse)
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
    }
}
