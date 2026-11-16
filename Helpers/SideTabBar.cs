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
    /// Runs <paramref name="apply"/> whenever the page's safe area changes: the bar moves between
    /// the side and the bottom when the phone is opened, closed or turned, which can come after
    /// the page got its new size.
    /// </summary>
    public static void Watch(Page page, Action apply)
    {
#if IOS
        page.Loaded += (_, _) =>
        {
            if ((page.Handler as IPlatformViewHandler)?.PlatformView is not UIKit.UIView view) return;
            if (view.Subviews.OfType<SafeAreaWatcher>().Any()) return;

            view.InsertSubview(new SafeAreaWatcher(apply) { Frame = view.Bounds }, 0);
        };
#endif
    }

    /// <summary>
    /// Gives the list (or the view that holds it) its margin and the room after its last row.
    /// A changed margin changes the list's width: its rows are laid out again, as iOS can keep
    /// the tiles at the width they had (the last column was then cut off at the page's edge).
    /// </summary>
    public static void Place(View host, View list, Thickness margin, double roomAfterLastRow)
    {
        if (host.Margin != margin)
        {
            host.Margin = margin;
            LayOutAgain(list);
        }

        SetRoomAfterLastRow(list, roomAfterLastRow);
    }

    static void LayOutAgain(View list)
    {
#if IOS
        // Once the list has its new size
        CoreFoundation.DispatchQueue.MainQueue.DispatchAsync(() =>
        {
            if ((list.Handler as IPlatformViewHandler)?.PlatformView is not UIKit.UIView view) return;

            foreach (var collection in Descendants(view).Prepend(view).OfType<UIKit.UICollectionView>())
            {
                collection.CollectionViewLayout.InvalidateLayout();
                collection.SetNeedsLayout();
            }
        });
#endif
    }

    /// <summary>
    /// Leaves room after a list's last row, so that it can be scrolled above the bar.
    /// </summary>
    static void SetRoomAfterLastRow(View list, double room)
    {
#if IOS
        if ((list.Handler as IPlatformViewHandler)?.PlatformView is not UIKit.UIView view) return;

        // A list never given room is left as it is: its inset is not this helper's to reset.
        var given = RoomGiven.TryGetValue(list, out var before) ? (double)before : 0;
        if (given == room) return;

        var scroller = view as UIKit.UIScrollView ?? Descendants(view).OfType<UIKit.UIScrollView>().FirstOrDefault();
        if (scroller is null) return;

        var inset = scroller.ContentInset;
        scroller.ContentInset = new UIKit.UIEdgeInsets(inset.Top, inset.Left, inset.Bottom + (nfloat)(room - given), inset.Right);
        RoomGiven.AddOrUpdate(list, room);
#endif
    }

#if IOS
    // The room each list was given, to take the same back
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<View, object> RoomGiven = [];

    // An unseen view as large as the page, which iOS tells about every change of the safe area.
    sealed class SafeAreaWatcher : UIKit.UIView
    {
        readonly Action _changed;

        public SafeAreaWatcher(Action changed)
        {
            _changed = changed;
            UserInteractionEnabled = false;
            BackgroundColor = UIKit.UIColor.Clear;
            AutoresizingMask = UIKit.UIViewAutoresizing.FlexibleWidth | UIKit.UIViewAutoresizing.FlexibleHeight;
        }

        // Applied after this layout pass: the change sets margins, which asks for another one.
        public override void SafeAreaInsetsDidChange()
        {
            base.SafeAreaInsetsDidChange();
            CoreFoundation.DispatchQueue.MainQueue.DispatchAsync(_changed);
        }
    }

    static IEnumerable<UIKit.UIView> Descendants(UIKit.UIView view) =>
        view.Subviews.SelectMany(child => new[] { child }.Concat(Descendants(child)));
#endif
}
