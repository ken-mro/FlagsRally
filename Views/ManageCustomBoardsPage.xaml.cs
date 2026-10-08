using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class ManageCustomBoardsPage : ContentPage
{
    public const string Route = "ManageCustomBoards";

    readonly ManageCustomBoardsPageViewModel _viewModel;

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
}
