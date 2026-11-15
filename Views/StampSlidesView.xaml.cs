using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class StampSlidesView : ContentView
{
    bool _moving;

    public StampSlidesView()
    {
        InitializeComponent();
    }

    /// <summary>The stamp on the page shown, for a popup that presses it down (StampPress).</summary>
    public View Stamp => stamp;

    /// <summary>Run once a page has slid in, such as pressing its stamp.</summary>
    public Func<Task>? PageShownAsync { get; set; }

    StampSlidesViewModel? ViewModel => BindingContext as StampSlidesViewModel;

    void OnSwipedLeft(object? sender, SwipedEventArgs e) => _ = SlideAsync(1);
    void OnSwipedRight(object? sender, SwipedEventArgs e) => _ = SlideAsync(-1);
    void OnNextTapped(object? sender, TappedEventArgs e) => _ = SlideAsync(1);

    // The stamp leaves to one side and the next page's comes in from the other
    async Task SlideAsync(int step)
    {
        if (_moving || ViewModel is not { } viewModel) return;
        var width = Math.Max(stage.Width, 300);
        _moving = true;
        try
        {
            await Task.WhenAll(stamp.TranslateToAsync(-step * width, 0, 220, Easing.CubicIn), stamp.FadeToAsync(0, 220));
            if (!viewModel.Move(step))
            {
                stamp.TranslationX = 0;
                stamp.Opacity = 1;
                return;
            }
            stamp.TranslationX = step * width;
            if (PageShownAsync is null)
            {
                // Nothing to press: the stamp fades in as it slides
                await Task.WhenAll(stamp.TranslateToAsync(0, 0, 220, Easing.CubicOut), stamp.FadeToAsync(1, 220));
            }
            else
            {
                // Comes in unseen, then is shown by PageShownAsync (pressed down)
                await stamp.TranslateToAsync(0, 0, 220, Easing.CubicOut);
                await PageShownAsync();
            }
        }
        finally
        {
            _moving = false;
        }
    }

    /// <summary>Confetti over the stamp, for a country or region stamped for the first time.</summary>
    public void Celebrate()
    {
        celebration.IsAnimationEnabled = false;
        celebration.Progress = TimeSpan.Zero;
        celebration.IsVisible = true;
        celebration.IsAnimationEnabled = true;
    }
}
