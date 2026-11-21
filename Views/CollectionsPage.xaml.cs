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
        SideTabBar.Watch(this, OnSafeAreaChanged);
        GridSpan.WatchWindow(this, UpdateColumns);
        BindingContext = _viewModel = vm;
    }

    // Two cards in a row, four on an opened foldable.
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        UpdateColumns();
        RunUnderSideTabBar();
    }

    void OnSafeAreaChanged()
    {
        UpdateColumns();
        RunUnderSideTabBar();
    }

    // The column count, from the page's width and whether the phone is opened. Worked out again
    // when the window changes size or the safe area moves, as either can come after the page's
    // new size when a foldable is opened or closed; iOS then lays the rows out again.
    void UpdateColumns()
    {
        if (Width <= 0) return;

        var span = GridSpan.ForCards(GridSpan.IsOpened(this));
        if (span == _viewModel.GridItemSpan) return;

        _viewModel.GridItemSpan = span;
        SideTabBar.LayOutAgain(Cards);
    }

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
