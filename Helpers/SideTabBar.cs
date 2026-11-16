namespace FlagsRally.Helpers;

/// <summary>
/// A tab bar that stands at the side of the page (iPhone Duo) instead of below it. iOS keeps the
/// whole strip beside it out of the safe area, from the top of the page to the bottom, though the
/// bar only fills its lower end: a list kept inside the safe area stops short of the page's edge
/// and looks cut off. Lists run under the bar instead, like under a tab bar at the bottom, with
/// room after the last row to scroll it clear of the bar.
/// </summary>
public static class SideTabBar
{
    /// <summary>
    /// What the bar's buttons take at the lower end of the strip, with the gap below them and a
    /// little air above (the bar itself measures 237 pt from the page's lower edge on iPhone Duo).
    /// </summary>
    public const double Height = 248;

    /// <summary>
    /// Whether the safe area's sides are those of a tab bar at the right: a phone on its side
    /// keeps both sides free (the camera at one, the same width at the other), the bar only one.
    /// </summary>
    public static bool IsAtSide(double leftInset, double rightInset) => rightInset > 0 && leftInset <= 0;

    /// <summary>
    /// The margin that takes a list from the safe area's edges out to the page's right and lower edge.
    /// </summary>
    public static Thickness MarginToEdges(Thickness margin, double rightInset, double bottomInset) =>
        new(margin.Left, margin.Top, margin.Right - rightInset, margin.Bottom - bottomInset);

    /// <summary>
    /// The safe area's right and bottom insets of the page when the tab bar stands at its side;
    /// null otherwise (and always on Android, whose tab bar is below the page).
    /// </summary>
    public static (double Right, double Bottom)? InsetsOf(Page page)
    {
#if IOS
        if ((page.Handler as IPlatformViewHandler)?.PlatformView is UIKit.UIView view)
        {
            var insets = view.SafeAreaInsets;
            if (IsAtSide(insets.Left, insets.Right)) return (insets.Right, insets.Bottom);
        }
#endif
        return null;
    }

    /// <summary>
    /// Leaves room after a list's last row, so that it can be scrolled above the bar.
    /// </summary>
    public static void SetRoomAfterLastRow(View list, double room)
    {
#if IOS
        if ((list.Handler as IPlatformViewHandler)?.PlatformView is not UIKit.UIView view) return;

        var scroller = view as UIKit.UIScrollView ?? Descendants(view).OfType<UIKit.UIScrollView>().FirstOrDefault();
        if (scroller is null || scroller.ContentInset.Bottom == (nfloat)room) return;

        var inset = scroller.ContentInset;
        scroller.ContentInset = new UIKit.UIEdgeInsets(inset.Top, inset.Left, (nfloat)room, inset.Right);
#endif
    }

#if IOS
    static IEnumerable<UIKit.UIView> Descendants(UIKit.UIView view) =>
        view.Subviews.SelectMany(child => new[] { child }.Concat(Descendants(child)));
#endif
}
