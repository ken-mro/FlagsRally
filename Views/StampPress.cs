namespace FlagsRally.Views;

/// <summary>
/// The "pressed down" entrance of a new stamp or collection piece: it comes in large, faint and
/// turned, is pressed flat and gives a little bounce. Shared by the popups after stamping.
/// </summary>
public static class StampPress
{
    // Hidden until pressed
    public static void Prepare(VisualElement view)
    {
        view.Scale = 1.4;
        view.Rotation = -12;
        view.Opacity = 0;
    }

    // Runs onPressed (the confetti) at the moment it touches the page
    public static async Task PressAsync(VisualElement view, Action? onPressed = null)
    {
        Prepare(view);
        await Task.WhenAll(
            view.ScaleToAsync(0.94, 260, Easing.CubicIn),
            view.RotateToAsync(0, 260, Easing.CubicIn),
            view.FadeToAsync(1, 200));
        onPressed?.Invoke();
        await view.ScaleToAsync(1, 140, Easing.SpringOut);
    }
}
