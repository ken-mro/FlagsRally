using FlagsRally.ViewModels;

namespace FlagsRally.Views;

public partial class CollectionsPage : ContentPage
{
    readonly CollectionsPageViewModel _viewModel;

    public CollectionsPage(CollectionsPageViewModel vm)
    {
        InitializeComponent();
        BindingContext = _viewModel = vm;
    }

    // Reload on every visit so progress reflects check-ins made on the map.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.Init();
    }

    // A tap selects the card; open it and clear the selection so the same card can be tapped again.
    void OnCardSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not CollectionCard card) return;

        Cards.SelectedItem = null;
        _viewModel.OpenCardCommand.Execute(card);
    }
}
