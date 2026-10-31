using CommunityToolkit.Maui.Views;
using System.ComponentModel;
using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class CustomLocationImagePopupView : Popup
{
    readonly CustomLocationImagePopupViewModel _viewModel;

    public CustomLocationImagePopupView(CustomLocationImagePopupViewModel viewModel)
    {
        InitializeComponent();
        viewModel.Popup = this;
        BindingContext = _viewModel = viewModel;

        // Looked at again: shown as it is
        if (!viewModel.IsAnimationEnabled) return;

        // Just checked in: the stamp is pressed down with confetti. It is downloaded, so the press waits
        // until it has loaded (or failed to) and does not land on an empty frame.
        StampPress.Prepare(stampImage);
        if (string.IsNullOrEmpty(viewModel.ImageUrl))
        {
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(350), () => _ = PressAsync());
        }
        else
        {
            stampImage.PropertyChanged += OnStampImagePropertyChanged;
            // A cached image may never report loading; the stamp must not stay hidden
            Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(3), () => _ = PressAsync());
        }
    }

    void OnStampImagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Image.IsLoading) || stampImage.IsLoading) return;
        _ = PressAsync();
    }

    bool _pressed;

    async Task PressAsync()
    {
        if (_pressed) return;
        _pressed = true;
        stampImage.PropertyChanged -= OnStampImagePropertyChanged;
        await StampPress.PressAsync(stampImage, StartCelebration);
    }

    void StartCelebration()
    {
        celebration.IsVisible = true;
        celebration.IsAnimationEnabled = true;
    }
}
