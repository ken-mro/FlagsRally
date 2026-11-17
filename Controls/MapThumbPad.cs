using FlagsRally.Helpers;
using FlagsRally.Resources;

namespace FlagsRally.Controls;

/// <summary>
/// The map's thumb controller. At rest it is a small round button. Put a thumb on it and two marks
/// show what it can do; the first slide chooses. A slide sideways (to the left, as the button is at the
/// right edge) opens the move pad, a disc: the map keeps moving the way the thumb leans, faster the
/// further out. A slide up or down opens the zoom pad, an upright bar with ＋ at the top and − at the
/// bottom: above the middle zooms in, below zooms out, faster the further from the middle. Either pad
/// opens with its middle under the thumb, so nothing moves until the thumb goes on. Lift the thumb and
/// the pad closes to the button again. The pads are see-through so the map shows under them, and only
/// the button takes touches, so the map round it can still be dragged. While the thumb is down the
/// controller reports, about 30 times a second, how far the map should move and how much the zoom
/// changed (<see cref="Moved"/>); the page turns that into camera moves with <see cref="MapPad"/>.
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
    const float ButtonShadowRoom = 8;
    // How far the drawing reaches from the button's centre: the zoom bar, opened a slide above it.
    const float Reach = (float)(MapPad.ZoomHalfHeight + MapPad.ModeSlop) + 8;
    const float KnobRadius = 14;
    const float PadOpacity = 0.6f;
    // The marks shown before the first slide: how far from the button's centre, and how large.
    const float MarkAway = 46;
    const float MarkRadius = 17;
    static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(33);

    static readonly Color Navy = AppColors.Get("Navy", "#182A52");
    static readonly Color NavyDeep = AppColors.Get("NavyDeep", "#0F1C3A");
    static readonly Color PaperCard = AppColors.Get("PaperCard", "#FBF8F0");
    static readonly Color PaperEdge = AppColors.Get("PaperEdge", "#DDD2BA");

    readonly IDispatcherTimer _timer;
    readonly GraphicsView _button;   // takes the touches
    readonly GraphicsView _pad;      // drawn round the button while a thumb is down
    PointF _finger;                  // from the button's centre
    PointF _middle;                  // the open pad's middle, from the button's centre
    MapPad.Mode? _mode;              // null until the first slide has chosen
    DateTime _lastTick;
    bool _touching;

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

        _button.StartInteraction += (_, e) => OnDown(e.Touches.FirstOrDefault());
        _button.DragInteraction += (_, e) => OnMove(e.Touches.FirstOrDefault());
        _button.EndInteraction += (_, _) => OnUp();
        _button.CancelInteraction += (_, _) => OnUp();
        Unloaded += (_, _) => _timer.Stop();
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
        _finger = _middle = FromCentre(touch);
        _mode = null;
        _lastTick = DateTime.UtcNow;
        _touching = true;
        Started?.Invoke(this, EventArgs.Empty);
        _timer.Start();
        Redraw();
    }

    void OnMove(PointF touch)
    {
        if (!_touching) return;
        _finger = FromCentre(touch);
        if (_mode is null)
        {
            // _middle is still where the thumb came down. The pad opens with its middle under the thumb.
            _mode = MapPad.ModeAt(_finger.X - _middle.X, _finger.Y - _middle.Y);
            if (_mode is not null) _middle = _finger;
        }
        Redraw();
    }

    void OnUp()
    {
        if (!_touching) return;
        _timer.Stop();
        Report();
        _touching = false;
        _mode = null;
        _finger = _middle = PointF.Zero;
        Redraw();
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
    static void DrawMoveMark(ICanvas canvas, PointF centre, float size)
    {
        canvas.FillColor = Navy;
        canvas.FillCircle(centre, size * 0.22f);
        foreach (var (x, y) in FourWays) DrawChevron(canvas, centre, x, y, tip: size, back: size * 0.7f, half: size * 0.3f);
    }

    static void DrawZoomMark(ICanvas canvas, PointF centre, float size)
    {
        DrawSign(canvas, centre.X, centre.Y - size * 0.55f, size * 0.42f, plus: true);
        DrawSign(canvas, centre.X, centre.Y + size * 0.6f, size * 0.42f, plus: false);
    }

    // The button at rest: a paper disc like the my-location button, with the move mark at its left
    // and the zoom mark at its top, the ways to slide for each.
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

            canvas.StrokeColor = Navy;
            canvas.StrokeSize = 1.6f;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeLineJoin = LineJoin.Round;
            DrawMoveMark(canvas, new PointF(centre.X - 9, centre.Y + 6), 8);
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
            var middle = new PointF(button.X + pad._middle.X, button.Y + pad._middle.Y);
            var finger = new PointF(pad._finger.X - pad._middle.X, pad._finger.Y - pad._middle.Y);
            switch (pad._mode)
            {
                case MapPad.Mode.Move: DrawMove(canvas, middle, finger); break;
                case MapPad.Mode.Zoom: DrawZoom(canvas, middle, finger); break;
                default: DrawChoice(canvas, button, new PointF(button.X + pad._finger.X, button.Y + pad._finger.Y)); break;
            }
        }

        // Before the first slide: where to slide for each pad.
        static void DrawChoice(ICanvas canvas, PointF button, PointF finger)
        {
            var move = new PointF(button.X - MarkAway, button.Y);
            var zoom = new PointF(button.X, button.Y - MarkAway);
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
            DrawMoveMark(canvas, move, 10);
            DrawZoomMark(canvas, zoom, 10);

            canvas.FillColor = Navy.WithAlpha(0.9f);
            canvas.FillCircle(finger, KnobRadius);
        }

        static void DrawMove(ICanvas canvas, PointF centre, PointF finger)
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

            var (kx, ky) = MapPad.KnobOffset(finger.X, finger.Y, radius - KnobRadius);
            canvas.FillColor = Navy.WithAlpha(0.9f);
            canvas.FillCircle(centre.X + (float)kx, centre.Y + (float)ky, KnobRadius);
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
