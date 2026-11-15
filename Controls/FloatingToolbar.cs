using FlagsRally.Helpers;
using Microsoft.Maui.Controls.Shapes;

namespace FlagsRally.Controls;

/// <summary>
/// The rounded bar of controls that floats just above the tab bar, over a page's stamps, tiles or
/// map (the passport and the two board pages). Each bar gets its own shadow.
/// </summary>
public class FloatingToolbar : Border
{
    public FloatingToolbar()
    {
        BackgroundColor = AppColors.Get("PaperCard", "#FBF8F0");
        Stroke = AppColors.Get("PaperEdge", "#DDD2BA");
        StrokeShape = new RoundRectangle { CornerRadius = 30 };
        StrokeThickness = 1;
        VerticalOptions = LayoutOptions.End;
        Shadow = new Shadow
        {
            Brush = AppColors.Get("NavyDeep", "#0F1C3A"),
            Opacity = 0.18f,
            Radius = 14,
            Offset = new Point(0, 4),
        };
    }
}
