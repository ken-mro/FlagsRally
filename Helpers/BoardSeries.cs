using System.Text.RegularExpressions;

namespace FlagsRally.Helpers;

/// <summary>
/// Which boards belong together on the map's filter, such as the manhole card issues
/// (マンホールカード第01弾 … 第29弾 and the 特別版 ones).
/// </summary>
public static partial class BoardSeries
{
    /// <summary>
    /// The series a board belongs to: the shared folder it was added from when that is known,
    /// otherwise its name without the issue number (see <see cref="FromName"/>).
    /// </summary>
    public static string Of(string boardName, string? folderName) =>
        string.IsNullOrWhiteSpace(folderName) ? FromName(boardName) : folderName.Trim();

    /// <summary>
    /// The name without what tells issues of a series apart: from 「第N弾」 or 「特別版」 on, or a
    /// trailing number ("Vol. 2", "-3", "#4"). A name with neither is its own series.
    /// </summary>
    public static string FromName(string boardName)
    {
        var name = boardName.Trim();

        var issue = JapaneseIssueRegex().Match(name);
        if (issue.Success && issue.Index > 0)
        {
            return TrimSeparators(name[..issue.Index]);
        }

        var number = TrailingNumberRegex().Match(name);
        if (number.Success && number.Index > 0)
        {
            var prefix = TrimSeparators(name[..number.Index]);
            if (prefix.Length > 0) return prefix;
        }
        return name;
    }

    static string TrimSeparators(string text) => text.TrimEnd(' ', '　', '-', '_', '・', '–', '(', '（');

    // 第1弾 / 第０１弾 / 特別版 …
    [GeneratedRegex(@"第[0-9０-９]+弾|特別版")]
    private static partial Regex JapaneseIssueRegex();

    // "Vol. 2", "vol2", "Part 3", "#4", "-5", " 6" at the end
    [GeneratedRegex(@"[\s\-_#]*(?:(?:vol|part|no)\.?\s*)?[0-9０-９]+$", RegexOptions.IgnoreCase)]
    private static partial Regex TrailingNumberRegex();
}
