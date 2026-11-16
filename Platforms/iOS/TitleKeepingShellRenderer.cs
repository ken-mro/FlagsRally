using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;

namespace FlagsRally.Platforms.iOS;

/// <summary>
/// Shell whose pages keep their title on the tabs opened after the first one. Where the tab bar
/// stands at the side (iPhone Duo), the navigation bar of a tab not shown yet has no height, and
/// .NET MAUI makes the title view's container that tall: the title and the buttons beside it
/// stayed invisible. The container is made again once the bar has its height.
/// </summary>
public class TitleKeepingShellRenderer : ShellRenderer
{
    protected override IShellPageRendererTracker CreatePageRendererTracker() =>
        new TitleKeepingPageTracker(this);
}

class TitleKeepingPageTracker(IShellContext context) : ShellPageRendererTracker(context)
{
    // Also runs when the page gets its size, which is after the bar got its own.
    protected override void UpdateTitleView()
    {
        base.UpdateTitleView();

        var item = ViewController?.NavigationItem;
        var bar = ViewController?.NavigationController?.NavigationBar;
        if (item?.TitleView is TitleViewContainer container && container.Frame.Height == 0 && bar?.Frame.Height > 0)
        {
            item.TitleView = null;
            base.UpdateTitleView();
        }
    }
}
