#if IOS || MACCATALYST
using UIKit;
#endif

namespace FlagsRally.Helpers;

/// <summary>
/// Where the app's tab bar and title bar are on screen, in window points. On iOS the tab bar is not
/// always along the bottom: on the iPhone Duo's outer screen it stands down the right side.
/// Elsewhere (Android) nothing is measured and callers keep their own layout.
/// </summary>
public static class TabBarGeometry
{
    /// <summary>
    /// The visible tab bar's frame, or null when it cannot be measured. Since iOS 26 the tabs float in a
    /// panel of their own, and the UITabBar's frame does not always follow it (after the iPhone Duo is
    /// folded or turned it can still describe the old place), so the frame around the tab buttons is used
    /// when they can be found.
    /// </summary>
    public static Rect? TabBar()
    {
#if IOS || MACCATALYST
        var buttons = TabButtonFrames();
        if (buttons.Count > 1)
        {
            // The platter around the buttons is a few points larger than they are
            const double platter = 4;
            var left = buttons.Min(f => f.Left) - platter;
            var top = buttons.Min(f => f.Top) - platter;
            return new Rect(left, top, buttons.Max(f => f.Right) + platter - left, buttons.Max(f => f.Bottom) + platter - top);
        }
        var window = KeyWindow();
        var bar = FindViewController<UITabBarController>(window?.RootViewController)?.TabBar;
        return bar is null || bar.Hidden || bar.Window is null ? null : FrameIn(bar, window!);
#else
        return null;
#endif
    }

    /// <summary>
    /// The centre of each tab button, in tab order, or an empty list when they cannot be found.
    /// The bar's own frame can be much larger than the buttons (the side bar spans the whole height).
    /// </summary>
    public static IReadOnlyList<Point> TabCentres()
    {
#if IOS || MACCATALYST
        var frames = TabButtonFrames();
        var vertical = frames.Count > 1 && frames.Max(f => f.Center.Y) - frames.Min(f => f.Center.Y)
                                          > frames.Max(f => f.Center.X) - frames.Min(f => f.Center.X);
        return frames.OrderBy(f => vertical ? f.Center.Y : f.Center.X).Select(f => f.Center).ToList();
#else
        return [];
#endif
    }

#if IOS || MACCATALYST
    // The visible tab buttons' frames in the window. They are private UIKit views (_UITabButton since
    // iOS 26, UITabBarButton before); since iOS 26 they sit in a floating platter, not in the UITabBar.
    static List<Rect> TabButtonFrames()
    {
        var frames = new List<Rect>();
        if (KeyWindow() is not { } window) return frames;
        void Collect(UIView view)
        {
            foreach (var sub in view.Subviews)
            {
                if (sub.Hidden || sub.Alpha <= 0) continue;
                var name = sub.Class.Name ?? "";
                if ((name == "_UITabButton" || name == "UITabBarButton") && IsOnTop(sub, window))
                {
                    var frame = FrameIn(sub, window);
                    // The selected tab's platter can draw a second copy of a button
                    if (!frames.Any(f => Math.Abs(f.Center.X - frame.Center.X) < 1 && Math.Abs(f.Center.Y - frame.Center.Y) < 1))
                        frames.Add(frame);
                }
                else
                {
                    Collect(sub);
                }
            }
        }
        Collect(window);
        return frames;
    }
#endif

    /// <summary>The bottom of the visible title bar, or null when it cannot be measured.</summary>
    public static double? TitleBarBottom()
    {
#if IOS || MACCATALYST
        var window = KeyWindow();
        var nav = FindViewController<UINavigationController>(window?.RootViewController, preferVisible: true);
        var bar = nav?.NavigationBar;
        if (bar is null || bar.Hidden || bar.Window is null) return null;
        var frame = FrameIn(bar, window!);
        return frame.Bottom;
#else
        return null;
#endif
    }

    /// <summary>Where a view is in the window once it has been laid out, or null when it cannot be measured.</summary>
    public static Rect? FrameInWindow(VisualElement element)
    {
#if IOS || MACCATALYST
        if (element.Handler?.PlatformView is not UIView view || view.Window is not { } window) return null;
        return FrameIn(view, window);
#else
        return null;
#endif
    }

    /// <summary>True when the bar is taller than it is wide, so its tabs run down the side.</summary>
    public static bool IsVertical(Rect bar) => bar.Height > bar.Width;

#if IOS || MACCATALYST
    static UIWindow? KeyWindow() =>
        UIApplication.SharedApplication.ConnectedScenes
            .OfType<UIWindowScene>()
            .Where(s => s.ActivationState == UISceneActivationState.ForegroundActive)
            .SelectMany(s => s.Windows)
            .FirstOrDefault(w => w.IsKeyWindow);

    // Whether a tap at the view's centre reaches it. After the iPhone Duo is turned or folded, UIKit can
    // keep a second set of tab buttons where the bar used to be; only the one in front takes taps.
    static bool IsOnTop(UIView view, UIWindow window)
    {
        // Tested in the tab bar controller's view: the how-to popup covers the window above it
        var root = FindViewController<UITabBarController>(window.RootViewController)?.View ?? window;
        var centre = view.ConvertPointToView(new CoreGraphics.CGPoint(view.Bounds.Width / 2, view.Bounds.Height / 2), root);
        for (var hit = root.HitTest(centre, null); hit is not null; hit = hit.Superview)
        {
            if (hit == view) return true;
        }
        return false;
    }

    // In the app's root view, the space MAUI lays out in. Not the UIWindow's own: on the iPhone Duo,
    // turned while open or folded, the window's space can be the screen turned half round.
    static Rect FrameIn(UIView view, UIWindow window)
    {
        var space = window.RootViewController?.View ?? window;
        var f = view.ConvertRectToView(view.Bounds, space);
        return new Rect(f.X, f.Y, f.Width, f.Height);
    }
    // Walks the controllers below the root (children and presented ones) for the first of the given type;
    // with preferVisible, the tab's selected controller is searched first.
    static T? FindViewController<T>(UIViewController? root, bool preferVisible = false) where T : UIViewController
    {
        if (root is null) return null;
        if (root is T found) return found;
        if (preferVisible && root is UITabBarController tabs && tabs.SelectedViewController is { } selected
            && FindViewController<T>(selected, preferVisible) is { } inSelected)
        {
            return inSelected;
        }
        foreach (var child in root.ChildViewControllers)
        {
            if (FindViewController<T>(child, preferVisible) is { } inChild) return inChild;
        }
        return root.PresentedViewController is { } presented && presented is not T
            ? FindViewController<T>(presented, preferVisible)
            : root.PresentedViewController as T;
    }
#endif
}
