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
    /// <summary>The visible tab bar's frame, or null when it cannot be measured.</summary>
    public static Rect? TabBar()
    {
#if IOS || MACCATALYST
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
        if (KeyWindow() is not { } window) return [];
        // The buttons are private UIKit views (_UITabButton since iOS 26, UITabBarButton before). Since
        // iOS 26 they are not inside the UITabBar but in a floating platter elsewhere in the window.
        var frames = new List<Rect>();
        void Collect(UIView view)
        {
            foreach (var sub in view.Subviews)
            {
                var name = sub.Class.Name ?? "";
                if (!sub.Hidden && sub.Alpha > 0 && (name == "_UITabButton" || name == "UITabBarButton"))
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
        var vertical = frames.Count > 1 && frames.Max(f => f.Center.Y) - frames.Min(f => f.Center.Y)
                                          > frames.Max(f => f.Center.X) - frames.Min(f => f.Center.X);
        return frames.OrderBy(f => vertical ? f.Center.Y : f.Center.X).Select(f => f.Center).ToList();
#else
        return [];
#endif
    }

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

    static Rect FrameIn(UIView view, UIWindow window)
    {
        var f = view.ConvertRectToView(view.Bounds, window);
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
