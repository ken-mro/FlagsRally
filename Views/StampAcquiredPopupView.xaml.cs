using CommunityToolkit.Maui.Views;
using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class StampAcquiredPopupView : Popup
{
    readonly StampAcquiredPopupViewModel _viewModel;
    readonly HashSet<int> _pressed = [];
    bool _moving;

    public StampAcquiredPopupView(StampAcquiredPopupViewModel viewModel)
    {
        InitializeComponent();
        viewModel.Popup = this;
        BindingContext = _viewModel = viewModel;
        // The first stamp is pressed once the popup is on screen
        stamp.Opacity = 0;
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(350), () => _ = PressAsync());
    }

    void OnSwipedLeft(object? sender, SwipedEventArgs e) => _ = SlideAsync(1);
    void OnSwipedRight(object? sender, SwipedEventArgs e) => _ = SlideAsync(-1);
    void OnNextTapped(object? sender, TappedEventArgs e) => _ = SlideAsync(1);

    // The stamp leaves to one side and the next page's comes in from the other, then is pressed
    async Task SlideAsync(int step)
    {
        if (_moving) return;
        var width = Math.Max(stage.Width, 300);
        _moving = true;
        try
        {
            await Task.WhenAll(stamp.TranslateToAsync(-step * width, 0, 220, Easing.CubicIn), stamp.FadeToAsync(0, 220));
            if (!_viewModel.Move(step))
            {
                stamp.TranslationX = 0;
                stamp.Opacity = 1;
                return;
            }
            stamp.TranslationX = step * width;
            await stamp.TranslateToAsync(0, 0, 220, Easing.CubicOut);
            await PressAsync();
        }
        finally
        {
            _moving = false;
        }
    }

    // The page's stamp pressed down with confetti (StampPress); once the last page's has been, OK comes on.
    async Task PressAsync()
    {
        var index = _viewModel.Index;
        if (_pressed.Contains(index))
        {
            stamp.Opacity = 1;
            stamp.Scale = 1;
            stamp.Rotation = 0;
        }
        else
        {
            await StampPress.PressAsync(stamp, Celebrate);
            _pressed.Add(index);
        }
        if (_pressed.Count == _viewModel.Slides.Count) _viewModel.CanClose = true;
    }

    void Celebrate()
    {
        celebration.IsAnimationEnabled = false;
        celebration.Progress = TimeSpan.Zero;
        celebration.IsVisible = true;
        celebration.IsAnimationEnabled = true;
    }
}
