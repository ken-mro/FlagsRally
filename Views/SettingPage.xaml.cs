using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class SettingPage : ContentPage
{
	readonly SettingPageViewModel _viewModel;

	public SettingPage(SettingPageViewModel vm)
	{
		InitializeComponent();
        BindingContext = _viewModel = vm;
    }

    // Boards may have been added or deleted since the page was last shown.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.RefreshCustomBoardsAsync();
    }
}