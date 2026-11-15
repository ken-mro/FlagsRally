namespace FlagsRally.Helpers;

/// <summary>How far through a board or a set of regions: the bar's share and the "3 / 47" beside it.</summary>
public static class VisitProgress
{
    public static double Ratio(int visited, int total) => total == 0 ? 0 : (double)visited / total;

    public static string Text(int visited, int total) => $"{visited} / {total}";
}
