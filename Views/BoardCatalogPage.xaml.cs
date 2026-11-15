using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class BoardCatalogPage : InitializingPage
{
    public const string Route = "BoardCatalog";

    readonly BoardCatalogPageViewModel _viewModel;

    public BoardCatalogPage(BoardCatalogPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = _viewModel = vm;
    }

    // A tap selects the row; open the folder or tick the board, then clear the selection so the row can be tapped again.
    void OnEntrySelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not BoardCatalogItem item) return;

        entries.SelectedItem = null;
        _viewModel.OpenEntryCommand.Execute(item);
    }

    // The system back button leaves a subfolder before it leaves the page.
    protected override bool OnBackButtonPressed() => _viewModel.GoUp() || base.OnBackButtonPressed();
}
