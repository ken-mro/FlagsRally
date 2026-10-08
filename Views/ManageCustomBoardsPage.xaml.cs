using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class ManageCustomBoardsPage : ContentPage
{
    public const string Route = "ManageCustomBoards";
    const uint ShiftAnimationMs = 120;
    const double HandleWidth = 64;

    readonly ManageCustomBoardsPageViewModel _viewModel;

    // Drag state
    View? _draggedRow;
    List<View> _rows = [];
    int _fromIndex;
    int _targetIndex;
    double _rowHeight;

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

    // Rows are dragged by their handle (the right-hand column). The gestures sit on the list
    // itself, which does not move, so the touch is not lost while a row follows the finger.
    Point? _pressedAt;

    private void OnListPointerPressed(object? sender, PointerEventArgs e)
    {
        _pressedAt = e.GetPosition(boardList);
    }

    private async void OnListPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _rows = boardList.Children.OfType<View>().ToList();
                if (_pressedAt is not Point start || _rows.Count == 0) return;
                if (start.X < boardList.Width - HandleWidth) return;   // not on the handle

                _rowHeight = _rows[0].Height;
                var index = (int)(start.Y / _rowHeight);
                if (index < 0 || index >= _rows.Count) return;

                _draggedRow = _rows[index];
                _fromIndex = _targetIndex = index;
                _draggedRow.ZIndex = 1;
                _draggedRow.Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.25f, Radius = 12, Offset = new Point(0, 4) };
                await _draggedRow.ScaleToAsync(1.02, ShiftAnimationMs);
                break;

            case GestureStatus.Running:
                if (_draggedRow is null || _rowHeight <= 0) return;
                _draggedRow.TranslationY = e.TotalY;

                var target = Math.Clamp(_fromIndex + (int)Math.Round(e.TotalY / _rowHeight), 0, _rows.Count - 1);
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
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _pressedAt = null;
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
}
