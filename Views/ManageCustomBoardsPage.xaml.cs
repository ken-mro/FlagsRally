using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class ManageCustomBoardsPage : ContentPage
{
    public const string Route = "ManageCustomBoards";
    const uint ShiftAnimationMs = 120;
    const double HandleWidth = 64;
    const double AutoScrollStep = 12;
    static readonly TimeSpan AutoScrollInterval = TimeSpan.FromMilliseconds(16);

    readonly ManageCustomBoardsPageViewModel _viewModel;

    // Drag state
    View? _draggedRow;
    List<View> _rows = [];
    int _fromIndex;
    int _targetIndex;
    double _rowHeight;
    double _fingerOffsetY;      // finger movement since the drag started (TotalY)
    double _scrollAtStart;
    IDispatcherTimer? _autoScrollTimer;
    Point? _pressedAt;

    public ManageCustomBoardsPage(ManageCustomBoardsPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = _viewModel = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.Init();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopAutoScroll();
    }

    // Rows are dragged by their handle (the right-hand column). The gestures sit on the list
    // itself, which does not move, so the touch is not lost while a row follows the finger.
    private void OnListPointerPressed(object? sender, PointerEventArgs e)
    {
        _pressedAt = e.GetPosition(boardList);
        if (_pressedAt is Point p && IsOnHandle(p))
        {
            KeepScrollViewFromTakingTheDrag();
        }
    }

    bool IsOnHandle(Point p) => p.X >= boardList.Width - HandleWidth;

    // A touch that starts on a handle reorders; anywhere else the list scrolls as usual.
    // Android resets this flag when the finger is lifted.
    void KeepScrollViewFromTakingTheDrag()
    {
#if ANDROID
        (boardList.Handler?.PlatformView as Android.Views.View)?.Parent?.RequestDisallowInterceptTouchEvent(true);
#endif
    }

    private async void OnListPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _rows = boardList.Children.OfType<View>().ToList();
                if (_pressedAt is not Point start || _rows.Count == 0 || !IsOnHandle(start)) return;

                _rowHeight = _rows[0].Height;
                var index = (int)(start.Y / _rowHeight);
                if (index < 0 || index >= _rows.Count) return;

                _draggedRow = _rows[index];
                _fromIndex = _targetIndex = index;
                _fingerOffsetY = 0;
                _scrollAtStart = scrollView.ScrollY;
                _draggedRow.ZIndex = 1;
                _draggedRow.Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.25f, Radius = 12, Offset = new Point(0, 4) };
                StartAutoScroll();
                await _draggedRow.ScaleToAsync(1.02, ShiftAnimationMs);
                break;

            case GestureStatus.Running:
                if (_draggedRow is null || _rowHeight <= 0) return;
                _fingerOffsetY = e.TotalY;
                UpdateDraggedRow();
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _pressedAt = null;
                StopAutoScroll();
                if (_draggedRow is null) return;
                var (from, to) = (_fromIndex, _targetIndex);

                foreach (var r in _rows)
                {
                    r.TranslationY = 0;
                }
                _draggedRow.Scale = 1;
                _draggedRow.ZIndex = 0;
                _draggedRow.Shadow = null!;
                _draggedRow = null;

                if (e.StatusType == GestureStatus.Completed)
                {
                    await _viewModel.MoveBoardAsync(from, to);
                }
                break;
        }
    }

    // Moves the dragged row with the finger (plus any auto-scroll) and slides the rows it passes.
    void UpdateDraggedRow()
    {
        if (_draggedRow is null) return;

        var offset = _fingerOffsetY + (scrollView.ScrollY - _scrollAtStart);
        _draggedRow.TranslationY = offset;

        var target = Math.Clamp(_fromIndex + (int)Math.Round(offset / _rowHeight), 0, _rows.Count - 1);
        if (target == _targetIndex) return;
        _targetIndex = target;

        for (int i = 0; i < _rows.Count; i++)
        {
            if (i == _fromIndex) continue;
            var shift = i > _fromIndex && i <= _targetIndex ? -_rowHeight
                      : i < _fromIndex && i >= _targetIndex ? _rowHeight
                      : 0;
            _ = _rows[i].TranslateToAsync(0, shift, ShiftAnimationMs, Easing.CubicOut);
        }
    }

    // While the dragged row is held near the top or bottom edge, keep scrolling the list.
    void StartAutoScroll()
    {
        _autoScrollTimer ??= Dispatcher.CreateTimer();
        _autoScrollTimer.Interval = AutoScrollInterval;
        _autoScrollTimer.Tick -= OnAutoScrollTick;
        _autoScrollTimer.Tick += OnAutoScrollTick;
        _autoScrollTimer.Start();
    }

    void StopAutoScroll() => _autoScrollTimer?.Stop();

    async void OnAutoScrollTick(object? sender, EventArgs e)
    {
        if (_draggedRow is null) return;

        var viewport = scrollView.Height;
        var maxScroll = Math.Max(0, scrollView.ContentSize.Height - viewport);
        var rowTopInViewport = _fromIndex * _rowHeight + _draggedRow.TranslationY - scrollView.ScrollY;
        // Edge zone: a fixed band at the top and bottom of the visible list.
        var edge = Math.Min(_rowHeight * 0.6, viewport / 4);
        var fingerInViewport = _pressedAt is Point start ? start.Y + _fingerOffsetY - _scrollAtStart : rowTopInViewport;

        double step = 0;
        if (fingerInViewport < edge && scrollView.ScrollY > 0)
        {
            step = -AutoScrollStep;
        }
        else if (fingerInViewport > viewport - edge && scrollView.ScrollY < maxScroll)
        {
            step = AutoScrollStep;
        }
        if (step == 0) return;

        var next = Math.Clamp(scrollView.ScrollY + step, 0, maxScroll);
        await scrollView.ScrollToAsync(0, next, false);
        UpdateDraggedRow();
    }
}
