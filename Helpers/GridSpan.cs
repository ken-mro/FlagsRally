namespace FlagsRally.Helpers;

/// <summary>
/// How many columns of tiles fit a page: one per 196 points, at least two. An opened foldable
/// shows twice the columns of one of its halves, so opening the phone doubles what it showed
/// closed; turning a phone on its side keeps the count by width.
/// </summary>
public static class GridSpan
{
    const double ColumnWidth = 196;

    /// <summary>
    /// A window whose shorter side is at least this many points is an opened foldable (or a
    /// tablet). A phone, upright or on its side, and a foldable's cover screen stay below it.
    /// </summary>
    public const double OpenedShortSide = 600;

    public static int For(double pageWidth) => Math.Max((int)(pageWidth / ColumnWidth), 2);

    public static bool IsOpened(double windowWidth, double windowHeight) =>
        Math.Min(windowWidth, windowHeight) >= OpenedShortSide;

    /// <summary>
    /// Opened, each half of the page counts as a closed phone and the grid runs across both, left
    /// to right from the top left. A wider page that fits more by width keeps that.
    /// </summary>
    public static int For(double pageWidth, bool opened) =>
        opened ? Math.Max(For(pageWidth / 2) * 2, For(pageWidth)) : For(pageWidth);

    /// <summary>The Collections cards: two in a row, four on an opened foldable.</summary>
    public static int ForCards(bool opened) => opened ? 4 : 2;

    /// <summary>Whether the page is on an opened foldable, by the size of its window.</summary>
    public static bool IsOpened(Page page) =>
        page.Window is { Width: > 0, Height: > 0 } window
            ? IsOpened(window.Width, window.Height)
            : IsOpened(page.Width, page.Height);

    /// <summary>
    /// Works out the page's column count again: <paramref name="setSpan"/> is given the page's width and
    /// whether the phone is opened. When the count (<paramref name="span"/>) changes, iOS lays the list's
    /// rows out again. Does nothing before the page has a width.
    /// </summary>
    public static void Update(Page page, View list, Func<int> span, Action<double, bool> setSpan)
    {
        if (page.Width <= 0) return;

        var before = span();
        setSpan(page.Width, IsOpened(page));
        if (span() != before) SideTabBar.LayOutAgain(list);
    }

    /// <summary>
    /// Runs <paramref name="update"/> whenever the page's window changes size. Opening or closing a
    /// foldable resizes the window, and the page can be given its new size before the window has
    /// its own: a count worked out then still followed the screen the phone had just left.
    /// </summary>
    public static void WatchWindow(Page page, Action update)
    {
        Window? watched = null;
        void OnWindowSizeChanged(object? sender, EventArgs e) => update();
        void Follow()
        {
            if (ReferenceEquals(watched, page.Window)) return;
            if (watched is not null) watched.SizeChanged -= OnWindowSizeChanged;
            watched = page.Window;
            if (watched is not null) watched.SizeChanged += OnWindowSizeChanged;
        }

        page.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(VisualElement.Window)) Follow();
        };
        Follow();
    }
}
