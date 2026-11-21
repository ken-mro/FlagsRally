using FlagsRally.Helpers;
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
        SideTabBar.Watch(this, LayOut);
        GridSpan.WatchWindow(this, LayOut);
        BindingContext = _viewModel = vm;
    }

    // Two cards in a row, four on an opened foldable.
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        LayOut();
    }

    // Opening or closing a foldable can give the page its new size before the window or the safe area
    // has its own, so this runs again when either changes.
    void LayOut()
    {
        UpdateColumns();
        RunUnderSideTabBar();
    }

    void UpdateColumns() =>
        GridSpan.Update(this, Cards, () => _viewModel.GridItemSpan, (width, opened) => _viewModel.GridItemSpan = GridSpan.ForCards(opened));

    // The list's margin from the XAML: on iOS, room at the bottom for the tab bar floating there.
    static readonly Thickness BesideMargin = new(8, 0);
    static readonly Thickness ListMargin = DeviceInfo.Platform == DevicePlatform.iOS ? new(8, 0, 8, 72) : BesideMargin;

    // Beside a tab bar at the side (iPhone Duo) the cards go on to the page's edge, under the bar,
    // and no room is kept for a tab bar at the bottom.
    void RunUnderSideTabBar()
    {
        var insets = SideTabBar.InsetsOf(this);
        SideTabBar.Place(Cards, Cards,
            insets is { } side ? SideTabBar.MarginToEdges(BesideMargin, side.Right, 0) : ListMargin,
            insets is null ? 0 : SideTabBar.Height);
    }

    // A tap selects the card; open it and clear the selection so the same card can be tapped again.
    void OnCardSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not CollectionCard card) return;

        Cards.SelectedItem = null;
        _viewModel.OpenCardCommand.Execute(card);
    }
}
