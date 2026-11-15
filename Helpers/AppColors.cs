namespace FlagsRally.Helpers;

/// <summary>
/// The colour tokens of Resources/Styles/Colors.xaml for code that draws or builds views itself.
/// </summary>
public static class AppColors
{
    /// <summary>The token's colour, or <paramref name="fallbackHex"/> without an app (unit tests).</summary>
    public static Color Get(string key, string fallbackHex) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : Color.FromArgb(fallbackHex);
}
