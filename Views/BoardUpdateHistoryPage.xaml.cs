using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class BoardUpdateHistoryPage : ContentPage
{
    public const string Route = "BoardUpdateHistory";

    readonly BoardUpdateHistoryPageViewModel _viewModel;

    public BoardUpdateHistoryPage(BoardUpdateHistoryPageViewModel vm)
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
