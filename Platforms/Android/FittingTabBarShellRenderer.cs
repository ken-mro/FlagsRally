using Android.Util;
using Android.Widget;
using Google.Android.Material.BottomNavigation;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using AView = Android.Views.View;
using AViewGroup = Android.Views.ViewGroup;

namespace FlagsRally.Platforms.Android;

/// <summary>
/// Shell whose tab labels always show in full. On a narrow screen (a foldable's cover screen)
/// with Japanese or a large system font, "コレクション" no longer fits its tab and Android
/// cuts it to "コレクシ…". The labels are then made smaller, all by the same amount.
/// </summary>
public class FittingTabBarShellRenderer : ShellRenderer
{
    protected override IShellBottomNavViewAppearanceTracker CreateBottomNavViewAppearanceTracker(ShellItem shellItem) =>
        new FittingTabLabelsTracker(this, shellItem);
}

class FittingTabLabelsTracker(IShellContext shellContext, ShellItem shellItem)
    : ShellBottomNavViewAppearanceTracker(shellContext, shellItem)
{
    // The labels' size from dimens.xml (both the selected and the other tabs).
    const float LabelSp = 12;
    // Room kept free on each side of a label inside its tab.
    const float SideMarginDp = 4;

    readonly HashSet<BottomNavigationView> _watched = [];

    public override void SetAppearance(BottomNavigationView bottomView, IShellAppearanceElement appearance)
    {
        base.SetAppearance(bottomView, appearance);
        Watch(bottomView);
    }

    public override void ResetAppearance(BottomNavigationView bottomView)
    {
        base.ResetAppearance(bottomView);
        Watch(bottomView);
    }

    void Watch(BottomNavigationView bottomView)
    {
        if (_watched.Add(bottomView))
        {
            // Folding or unfolding changes the width of the tabs.
            bottomView.LayoutChange += (_, _) => bottomView.Post(() => FitLabels(bottomView));
        }
        bottomView.Post(() => FitLabels(bottomView));
    }

    static void FitLabels(BottomNavigationView bottomView)
    {
        if (bottomView.ChildCount == 0 || bottomView.GetChildAt(0) is not AViewGroup menuView) return;

        var metrics = bottomView.Resources!.DisplayMetrics!;
        var fullSize = TypedValue.ApplyDimension(ComplexUnitType.Sp, LabelSp, metrics);
        var margin = TypedValue.ApplyDimension(ComplexUnitType.Dip, SideMarginDp, metrics);

        var labels = new List<TextView>();
        var scale = 1f;
        for (var i = 0; i < menuView.ChildCount; i++)
        {
            var item = menuView.GetChildAt(i)!;
            var available = item.Width - item.PaddingLeft - item.PaddingRight - 2 * margin;
            if (available <= 0) return;

            foreach (var label in FindLabels(item))
            {
                labels.Add(label);
                var paint = new global::Android.Text.TextPaint(label.Paint) { TextSize = fullSize };
                var needed = paint.MeasureText(label.Text ?? "");
                if (needed > available) scale = Math.Min(scale, available / needed);
            }
        }

        foreach (var label in labels)
        {
            label.SetTextSize(ComplexUnitType.Px, fullSize * scale);
        }
    }

    static IEnumerable<TextView> FindLabels(AView view)
    {
        if (view is TextView label)
        {
            yield return label;
        }
        else if (view is AViewGroup group)
        {
            for (var i = 0; i < group.ChildCount; i++)
            {
                foreach (var child in FindLabels(group.GetChildAt(i)!)) yield return child;
            }
        }
    }
}
