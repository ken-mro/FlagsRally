namespace FlagsRally.Helpers;

/// <summary>How many columns of tiles fit a page: one per 196 points, at least two.</summary>
public static class GridSpan
{
    public static int For(double pageWidth) => Math.Max((int)pageWidth / 196, 2);
}
