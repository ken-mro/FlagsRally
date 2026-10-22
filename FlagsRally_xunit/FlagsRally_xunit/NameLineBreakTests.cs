using FlagsRally.Helpers;

namespace FlagsRallyTests.Helpers;

public class NameLineBreakTests
{
    [Theory]
    [InlineData("Saint Vincent and the Grenadines", "Saint Vincent and", "the Grenadines")]
    [InlineData("Provence-Alpes-Côte d'Azur", "Provence-Alpes-", "Côte d'Azur")]
    [InlineData("ボスニア・ヘルツェゴビナ", "ボスニア・", "ヘルツェゴビナ")]
    [InlineData("セントビンセント及びグレナディーン諸島", "セントビンセント及び", "グレナディーン諸島")]
    [InlineData("セントビンセントおよびグレナディーン諸島", "セントビンセントおよび", "グレナディーン諸島")]
    [InlineData("プロヴァンス＝アルプ＝コート・ダジュール地域圏", "プロヴァンス＝アルプ＝", "コート・ダジュール地域圏")]
    public void Names_break_where_a_word_ends_into_even_lines(string name, string first, string second)
    {
        Assert.Equal((first, second, false), NameLineBreak.SplitInTwo(name));
    }

    [Fact]
    public void A_Japanese_name_without_marks_breaks_near_the_middle_not_before_a_long_vowel()
    {
        var split = NameLineBreak.SplitInTwo("グレナディーンディーン");
        Assert.NotNull(split);
        Assert.False(split.Value.Second.StartsWith('ー'));
        Assert.Equal("グレナディーンディーン", split.Value.First + split.Value.Second);
        Assert.True(split.Value.InWord);
    }

    [Theory]
    [InlineData("Grenadines")]
    [InlineData("A")]
    public void One_word_is_not_broken(string name)
    {
        Assert.Null(NameLineBreak.SplitInTwo(name));
    }
}
