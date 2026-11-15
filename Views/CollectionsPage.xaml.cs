using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class CollectionsPage : InitializingPage
{
    readonly CollectionsPageViewModel _viewModel;

    // For the section headings, which reach the page's commands through the page.
    public CollectionsPageViewModel ViewModel => _viewModel;

    public CollectionsPage(CollectionsPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = _viewModel = vm;
    }

    // A tap selects the card; open it and clear the selection so the same card can be tapped again.
    void OnCardSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not CollectionCard card) return;

        Cards.SelectedItem = null;
        _viewModel.OpenCardCommand.Execute(card);
    }
}
