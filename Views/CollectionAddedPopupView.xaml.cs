using CommunityToolkit.Maui.Views;
using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class CollectionAddedPopupView : Popup
{
    readonly CollectionAddedPopupViewModel _viewModel;

    public CollectionAddedPopupView(CollectionAddedPopupViewModel viewModel)
    {
        InitializeComponent();
        viewModel.Popup = this;
        BindingContext = _viewModel = viewModel;
        progress.Progress = viewModel.ProgressBefore;
        StampPress.Prepare(tile);
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(350), () => _ = ShowAsync());
    }

    // The region's tile is pressed down, then the bar moves on by the new region and OK comes on
    async Task ShowAsync()
    {
        await StampPress.PressAsync(tile, Celebrate);
        await progress.ProgressTo(_viewModel.ProgressAfter, 500, Easing.CubicOut);
        _viewModel.CanClose = true;
    }

    void Celebrate()
    {
        celebration.IsVisible = true;
        celebration.IsAnimationEnabled = true;
    }
}
