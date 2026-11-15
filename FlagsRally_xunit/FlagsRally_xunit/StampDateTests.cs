using System.Globalization;
using FlagsRally.Helpers;

namespace FlagsRallyTests.Helpers;

public class StampDateTests
{
    // Pins and board tiles show the month in English even when the app is in Japanese
    [Fact]
    public void Dates_are_in_English_whatever_the_language()
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ja-JP");
            var date = new DateTime(2026, 10, 9);
            Assert.Equal("09 Oct 2026", StampDate.Format(date));
            Assert.Equal("09  Oct  2026", StampDate.FormatSpaced(date));
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }
}
