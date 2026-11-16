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
}
