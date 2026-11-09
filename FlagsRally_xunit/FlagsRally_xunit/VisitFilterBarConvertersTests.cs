using System.Globalization;
using FlagsRally.Converters;
using Microsoft.Maui.Graphics;

namespace FlagsRallyTests.Converters;

public class VisitFilterBarConvertersTests
{
    [Fact]
    public void The_count_follows_the_label_after_a_space_the_line_can_break_at()
    {
        Assert.Equal(" 1000", CountSuffixConverter.Format(true, 1000));
        // U+2002 (en space) is a break opportunity, unlike a no-break space, so a count that does not
        // fit beside "Not visited" goes to the next line instead of being cut off.
        Assert.NotEqual(' ', CountSuffixConverter.Separator[0]);
    }

    [Fact]
    public void No_count_is_shown_when_the_bar_hides_counts()
    {
        Assert.Equal(string.Empty, CountSuffixConverter.Format(false, 1000));
    }

    [Fact]
    public void The_multi_binding_reads_whether_counts_are_shown_and_the_count()
    {
        var converter = new CountSuffixConverter();
        Assert.Equal(" 225", converter.Convert([true, 225], typeof(string), null, CultureInfo.InvariantCulture));
        Assert.Equal(string.Empty, converter.Convert([false, 225], typeof(string), null, CultureInfo.InvariantCulture));
        // Before the bindings resolve, the values are not yet a bool and an int.
        Assert.Equal(string.Empty, converter.Convert([null!, null!], typeof(string), null, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void The_selected_part_is_drawn_in_the_selected_colour()
    {
        var converter = new BoolToColorConverter { TrueColor = Colors.White, FalseColor = Colors.Navy };
        Assert.Equal(Colors.White, converter.Convert(true, typeof(Color), null, CultureInfo.InvariantCulture));
        Assert.Equal(Colors.Navy, converter.Convert(false, typeof(Color), null, CultureInfo.InvariantCulture));
        Assert.Equal(Colors.Navy, converter.Convert(null, typeof(Color), null, CultureInfo.InvariantCulture));
    }
}
