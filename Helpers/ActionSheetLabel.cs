namespace FlagsRally.Helpers;

/// <summary>An action sheet's choices, where the one in use carries a tick (toolbars only show an icon).</summary>
public static class ActionSheetLabel
{
    public static string Ticked(bool isTicked, string label) => isTicked ? $"✓ {label}" : label;
}
