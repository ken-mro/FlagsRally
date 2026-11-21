using FlagsRally.Helpers;
using FlagsRally.Resources;

namespace FlagsRally.Controls;

/// <summary>
/// The closed map ring controller: a paper disc with the four-way move mark, the size of the map's
/// other round buttons and in their column. Tapping it opens the controller it belongs to
/// (<see cref="MapRingController.Button"/>), which grows out of it.
/// </summary>
public class MapRingButton : GraphicsView
{
    /// <summary>The button's size, the same as the map's other round buttons.</summary>
    public const float Size = 48;
    const float ShadowRoom = 8;

    public event EventHandler? Tapped;

    public MapRingButton()
    {
        // Room for the shadow round the button.
        WidthRequest = Size + ShadowRoom * 2;
        HeightRequest = Size + ShadowRoom * 2;
        Margin = new Thickness(-ShadowRoom);
        BackgroundColor = Colors.Transparent;
        Drawable = new ButtonDrawable();
        SemanticProperties.SetDescription(this, AppResources.MapRing);
        SemanticProperties.SetHint(this, AppResources.MapRingHint);
        EndInteraction += (_, e) =>
        {
            if (e.IsInsideBounds) Tapped?.Invoke(this, EventArgs.Empty);
        };
    }

    sealed class ButtonDrawable : IDrawable
    {
        static readonly Color NavyDeep = AppColors.Get("NavyDeep", "#0F1C3A");
        static readonly Color Navy = AppColors.Get("Navy", "#182A52");
        static readonly Color PaperCard = AppColors.Get("PaperCard", "#FBF8F0");
        static readonly Color PaperEdge = AppColors.Get("PaperEdge", "#DDD2BA");

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var centre = dirtyRect.Center;
            var radius = Size / 2;
            canvas.SaveState();
            canvas.SetShadow(new SizeF(0, 2), 6, NavyDeep.WithAlpha(0.18f));
            canvas.FillColor = PaperCard;
            canvas.FillCircle(centre, radius);
            canvas.RestoreState();
            canvas.StrokeColor = PaperEdge;
            canvas.StrokeSize = 1;
            canvas.DrawCircle(centre, radius - 0.5f);
            MapRingController.DrawMoveMark(canvas, centre, 11, Navy, 1.8f);
        }
    }
}
