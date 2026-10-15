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

        // The stamp is downloaded, so the confetti waits until it has loaded (or failed to).
        if (string.IsNullOrEmpty(viewModel.ImageUrl))
        {
            StartCelebration();
        }
        else
        {
            stampImage.PropertyChanged += OnStampImagePropertyChanged;
        }
    }

    void OnStampImagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Image.IsLoading) || stampImage.IsLoading) return;

        stampImage.PropertyChanged -= OnStampImagePropertyChanged;
        StartCelebration();
    }

    void StartCelebration()
    {
        if (!_viewModel.IsAnimationEnabled) return;

        celebration.IsVisible = true;
        celebration.IsAnimationEnabled = true;
    }
}