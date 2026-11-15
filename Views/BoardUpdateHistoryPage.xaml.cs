using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class BoardUpdateHistoryPage : InitializingPage
{
    public const string Route = "BoardUpdateHistory";

    readonly BoardUpdateHistoryPageViewModel _viewModel;

    public BoardUpdateHistoryPage(BoardUpdateHistoryPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = _viewModel = vm;
    }

}
