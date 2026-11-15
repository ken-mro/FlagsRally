using System.Globalization;

namespace FlagsRally.Helpers;

/// <summary>
/// The date on a pin's card and a board tile, in English ("09 Oct 2026") whatever the language.
/// The passport stamp's date is not formatted here: it follows the language on purpose
/// (ArrivalLocation.ArrivalDateString).
/// </summary>
public static class StampDate
{
    static readonly CultureInfo English = CultureInfo.CreateSpecificCulture("en-US");

    /// <summary>"09 Oct 2026", for a pin's card.</summary>
    public static string Format(DateTime date) => date.ToString("dd MMM yyyy", English);

    /// <summary>"09  Oct  2026", spaced out for the date line on a board tile.</summary>
    public static string FormatSpaced(DateTime date) => date.ToString("dd  MMM  yyyy", English);
}
