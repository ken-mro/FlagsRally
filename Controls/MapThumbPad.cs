using FlagsRally.Helpers;
using FlagsRally.Resources;

namespace FlagsRally.Controls;

/// <summary>
/// One of the map's two thumb controllers: <see cref="PadKind.Move"/> moves the map,
/// <see cref="PadKind.Zoom"/> zooms it. At rest it is a small round button. Put a thumb on it and its
/// pad opens round the thumb; lift the thumb and it closes to the button again. The move pad is a
/// disc: the map keeps moving the way the thumb leans, faster the further out. The zoom pad is an
/// upright bar with ＋ at the top and − at the bottom: above the middle zooms in, below zooms out,
/// faster the further from the middle. The pads are see-through so the map shows under them, and only
/// the button takes touches, so the map round it can still be dragged. While the thumb is down the
/// pad reports, about 30 times a second, how far the map should move and how much the zoom changed
/// (<see cref="Moved"/>); the page turns that into camera moves with <see cref="MapPad"/>.
/// </summary>
public class MapThumbPad : Grid
{
    public enum PadKind { Move, Zoom }

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
    const float ButtonShadowRoom = 8;
    // Room round the open pad for its outline.
    const float EdgeRoom = 2;
    const float KnobRadius = 14;
    const float PadOpacity = 0.6f;
    static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(33);

    static readonly Color Navy = AppColors.Get("Navy", "#182A52");
    static readonly Color NavyDeep = AppColors.Get("NavyDeep", "#0F1C3A");
    static readonly Color PaperCard = AppColors.Get("PaperCard", "#FBF8F0");
    static readonly Color PaperEdge = AppColors.Get("PaperEdge", "#DDD2BA");

    readonly IDispatcherTimer _timer;
    readonly GraphicsView _button;   // takes the touches
    readonly GraphicsView _pad;      // drawn round the button while a thumb is down
    PadKind _kind;
    PointF _finger;                  // from the button's centre
    DateTime _lastTick;
    bool _touching;

    public MapThumbPad()
    {
        WidthRequest = ButtonSize;
        HeightRequest = ButtonSize;

        // Larger than the button it is centred on, and drawn over what is next to it.
        _pad = new GraphicsView
        {
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
        Children.Add(_pad);
        Children.Add(_button);
        ApplyKind();

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = Tick;
        _timer.Tick += (_, _) => Report();

        _button.StartInteraction += (_, e) => OnDown(e.Touches.FirstOrDefault());
        _button.DragInteraction += (_, e) => OnMove(e.Touches.FirstOrDefault());
        _button.EndInteraction += (_, _) => OnUp();
        _button.CancelInteraction += (_, _) => OnUp();
        Loaded += (_, _) => OpenChanged += OnOpenChanged;
        Unloaded += (_, _) =>
        {
            OpenChanged -= OnOpenChanged;
            _timer.Stop();
        };
    }

    // The pad that is open, if any. The other controller's button is not drawn meanwhile, so it does
    // not show on top of the open pad. (Raising the open pad with ZIndex instead makes Android take
    // the view out and put it back, which stops its timer in the middle of the touch.)
    static MapThumbPad? _open;
    static event Action? OpenChanged;

    void OnOpenChanged() => _button.Invalidate();

    static void SetOpen(MapThumbPad? pad)
    {
        _open = pad;
        OpenChanged?.Invoke();
    }

    /// <summary>What this controller does; set once, in XAML.</summary>
    public PadKind Kind
    {
        get => _kind;
        set
        {
            _kind = value;
            ApplyKind();
        }
    }

    void ApplyKind()
    {
        var (halfWidth, halfHeight) = _kind == PadKind.Zoom
            ? (MapPad.ZoomHalfWidth, MapPad.ZoomHalfHeight)
            : (MapPad.Radius, MapPad.Radius);
        var width = (halfWidth + EdgeRoom) * 2;
        var height = (halfHeight + EdgeRoom) * 2;
        _pad.WidthRequest = width;
        _pad.HeightRequest = height;
        _pad.Margin = new Thickness((ButtonSize - width) / 2, (ButtonSize - height) / 2);
        var zoom = _kind == PadKind.Zoom;
        SemanticProperties.SetDescription(_button, zoom ? AppResources.MapZoomPad : AppResources.MapPad);
        SemanticProperties.SetHint(_button, zoom ? AppResources.MapZoomPadHint : AppResources.MapPadHint);
        _button.Invalidate();
    }

    PointF FromCentre(PointF touch) => new(touch.X - (float)(_button.Width / 2), touch.Y - (float)(_button.Height / 2));

    void Redraw()
    {
        _pad.IsVisible = _touching;
        _pad.Invalidate();
        _button.Invalidate();
    }

    void OnDown(PointF touch)
    {
        // The pad opens centred on the button, so a thumb that came down off its centre starts
        // a little way out. The dead zones take that up.
        _finger = FromCentre(touch);
        _lastTick = DateTime.UtcNow;
        _touching = true;
        SetOpen(this);
        Started?.Invoke(this, EventArgs.Empty);
        _timer.Start();
        Redraw();
    }

    void OnMove(PointF touch)
    {
        if (!_touching) return;
        _finger = FromCentre(touch);
        Redraw();
    }

    void OnUp()
    {
        if (!_touching) return;
        _timer.Stop();
        Report();
        _touching = false;
        if (_open == this) SetOpen(null);
        _finger = PointF.Zero;
        Redraw();
    }

    void Report()
    {
        var now = DateTime.UtcNow;
        var seconds = Math.Min((now - _lastTick).TotalSeconds, 0.1);
        _lastTick = now;

        if (_kind == PadKind.Zoom)
        {
            var speed = MapPad.ZoomSpeed(_finger.Y);
            if (speed != 0) Moved?.Invoke(this, new MoveEventArgs(0, 0, speed * seconds));
            return;
        }
        var (vx, vy) = MapPad.PanVelocity(_finger.X, _finger.Y);
        if (vx != 0 || vy != 0) Moved?.Invoke(this, new MoveEventArgs(vx * seconds, vy * seconds, 0));
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

    // The button at rest: a paper disc like the my-location button. The move button shows a knob and
    // the four ways it moves the map; the zoom button shows ＋ over −.
    sealed class ButtonDrawable(MapThumbPad pad) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (_open is not null) return;
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

            canvas.StrokeColor = Navy;
            canvas.StrokeSize = 2;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeLineJoin = LineJoin.Round;
            if (pad._kind == PadKind.Zoom)
            {
                DrawSign(canvas, centre.X, centre.Y - 9, 5.5f, plus: true);
                DrawSign(canvas, centre.X, centre.Y + 10, 5.5f, plus: false);
                canvas.StrokeColor = PaperEdge;
                canvas.StrokeSize = 1;
                canvas.DrawLine(centre.X - 9, centre.Y + 1.5f, centre.X + 9, centre.Y + 1.5f);
                return;
            }
            canvas.FillColor = Navy;
            canvas.FillCircle(centre, 6);
            foreach (var (x, y) in FourWays) DrawChevron(canvas, centre, x, y, tip: 17, back: 13, half: 4);
        }
    }

    // The open pad. See-through, so the map shows under it; no shadow, which would show through the paper.
    sealed class PadDrawable(MapThumbPad pad) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeLineJoin = LineJoin.Round;
            if (pad._kind == PadKind.Zoom) DrawZoom(canvas, dirtyRect.Center);
            else DrawMove(canvas, dirtyRect.Center);
        }

        void DrawMove(ICanvas canvas, PointF centre)
        {
            var radius = (float)MapPad.Radius;
            canvas.FillColor = PaperCard.WithAlpha(PadOpacity);
            canvas.FillCircle(centre, radius);
            canvas.StrokeColor = Navy.WithAlpha(0.45f);
            canvas.StrokeSize = 1;
            canvas.DrawCircle(centre, radius);

            // The four ways, at the rim.
            canvas.StrokeColor = Navy.WithAlpha(0.7f);
            canvas.StrokeSize = 2;
            foreach (var (x, y) in FourWays) DrawChevron(canvas, centre, x, y, tip: radius - 7, back: radius - 12, half: 5);

            var (kx, ky) = MapPad.KnobOffset(pad._finger.X, pad._finger.Y, radius - KnobRadius);
            canvas.FillColor = Navy.WithAlpha(0.9f);
            canvas.FillCircle(centre.X + (float)kx, centre.Y + (float)ky, KnobRadius);
        }

        void DrawZoom(ICanvas canvas, PointF centre)
        {
            var halfWidth = (float)MapPad.ZoomHalfWidth;
            var halfHeight = (float)MapPad.ZoomHalfHeight;
            var bar = new RectF(centre.X - halfWidth, centre.Y - halfHeight, halfWidth * 2, halfHeight * 2);
            canvas.FillColor = PaperCard.WithAlpha(PadOpacity);
            canvas.FillRoundedRectangle(bar, halfWidth);
            canvas.StrokeColor = Navy.WithAlpha(0.45f);
            canvas.StrokeSize = 1;
            canvas.DrawRoundedRectangle(bar, halfWidth);

            var speed = MapPad.ZoomSpeed(pad._finger.Y);
            var knobY = Math.Clamp(pad._finger.Y, -(halfHeight - KnobRadius), halfHeight - KnobRadius);

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
