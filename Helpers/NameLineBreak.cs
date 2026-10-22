using FlagsRally.Converters;

namespace FlagsRally.Helpers;

/// <summary>
/// Where a place name that does not fit on one line is broken into two: at a space or after a
/// hyphen or "・" / "＝", whichever gives the most even lines, so a word is never cut in the
/// middle ("Grena / dines") and no line is left with a single character ("ヘルツェゴビ / ナ").
/// A Japanese name without such a mark is broken near its middle, keeping a long-vowel mark or a
/// small kana with the character before it.
/// </summary>
public static class NameLineBreak
{
    const string BreakAfter = "-‐・＝=／/";
    const string NotAtLineStart = "ーァィゥェォッャュョヮヵヶぁぃぅぇぉっゃゅょゎ、。・）」』〕】,.)";

    /// <summary>
    /// The two lines, or null when the name cannot be broken (one word). InWord says the break is
    /// inside a Japanese word, for want of a better place: worth avoiding while one line still fits.
    /// </summary>
    public static (string First, string Second, bool InWord)? SplitInTwo(string name)
    {
        var text = name.Trim();
        if (text.Length < 2) return null;

        (string, string)? best = null;
        var bestWidth = double.MaxValue;
        void Consider(string first, string second)
        {
            first = first.TrimEnd(); second = second.TrimStart();
            if (first.Length == 0 || second.Length == 0) return;
            var width = Math.Max(Width(first), Width(second));
            if (width < bestWidth) (best, bestWidth) = ((first, second), width);
        }

        for (var i = 1; i < text.Length; i++)
        {
            if (text[i] == ' ') Consider(text[..i], text[(i + 1)..]);
            else if (BreakAfter.Contains(text[i - 1]) && text[i] != ' ') Consider(text[..i], text[i..]);
            else if (i >= 2 && text[(i - 2)..i] == "及び") Consider(text[..i], text[i..]);
        }
        if (best is { } atMark) return (atMark.Item1, atMark.Item2, false);
        if (!text.ContainsJapaneseCharacters()) return null;

        // No mark to break at: near the middle
        for (var i = 1; i < text.Length; i++)
        {
            if (!NotAtLineStart.Contains(text[i])) Consider(text[..i], text[i..]);
        }
        return best is { } inWord ? (inWord.Item1, inWord.Item2, true) : null;
    }

    // Japanese characters are about twice as wide as Latin letters
    static double Width(string text) => text.Sum(c => c > 0x2E80 ? 2.0 : 1.0);
}
