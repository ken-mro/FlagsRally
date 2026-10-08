using System.Net;
using System.Text.RegularExpressions;

namespace FlagsRally.Services;

/// <summary>
/// A file or folder in the shared board folder on Google Drive.
/// </summary>
public record DriveEntry(string Id, string Name, bool IsFolder)
{
    public bool IsBoardFile => !IsFolder && CustomBoardFile.IsBoardFile(Name);

    /// <summary>
    /// The name without ".json" / ".json.encrypted".
    /// </summary>
    public string DisplayName => IsFolder ? Name : CustomBoardFile.StripExtension(Name);
}

public static class CustomBoardFile
{
    static readonly string[] Extensions = [".json.encrypted", ".json"];

    public static bool IsBoardFile(string fileName) => Extensions.Any(x => fileName.EndsWith(x, StringComparison.OrdinalIgnoreCase));

    public static string StripExtension(string fileName) =>
        Extensions.FirstOrDefault(x => fileName.EndsWith(x, StringComparison.OrdinalIgnoreCase)) is string ext
            ? fileName[..^ext.Length]
            : fileName;
}

/// <summary>
/// Lists and downloads the boards published in a public Google Drive folder.
/// Public folders can be read without an API key through Drive's embeddable folder view.
/// </summary>
public partial class DriveBoardCatalog
{
    /// <summary>
    /// The public folder where FlagsRally boards are shared.
    /// </summary>
    public const string RootFolderId = "1wclZZ_udWOVLWwIcbX8CsgPIMdWAJ-Rg";

    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    public async Task<List<DriveEntry>> ListAsync(string folderId, CancellationToken cancellationToken = default)
    {
        var html = await Http.GetStringAsync($"https://drive.google.com/embeddedfolderview?id={Uri.EscapeDataString(folderId)}", cancellationToken);
        return Parse(html);
    }

    public async Task<Stream> DownloadAsync(string fileId, CancellationToken cancellationToken = default)
    {
        var bytes = await Http.GetByteArrayAsync($"https://drive.google.com/uc?export=download&id={Uri.EscapeDataString(fileId)}", cancellationToken);
        return new MemoryStream(bytes);
    }

    /// <summary>
    /// Reads the entries of an embedded folder view: folders first, then files, each by name.
    /// </summary>
    public static List<DriveEntry> Parse(string html)
    {
        var entries = new List<DriveEntry>();
        foreach (Match match in EntryRegex().Matches(html))
        {
            var href = match.Groups["href"].Value;
            var name = WebUtility.HtmlDecode(match.Groups["title"].Value).Trim();
            if (FolderRegex().Match(href) is { Success: true } folder)
            {
                entries.Add(new DriveEntry(folder.Groups[1].Value, name, IsFolder: true));
            }
            else if (FileRegex().Match(href) is { Success: true } file)
            {
                entries.Add(new DriveEntry(file.Groups[1].Value, name, IsFolder: false));
            }
        }
        return entries.OrderByDescending(x => x.IsFolder).ThenBy(x => x.Name, StringComparer.CurrentCulture).ToList();
    }

    [GeneratedRegex("""<div class="flip-entry-info"><a href="(?<href>[^"]+)"[^>]*>.*?<div class="flip-entry-title">(?<title>.*?)</div>""", RegexOptions.Singleline)]
    private static partial Regex EntryRegex();

    [GeneratedRegex(@"/drive/folders/([\w-]+)")]
    private static partial Regex FolderRegex();

    [GeneratedRegex(@"/file/d/([\w-]+)")]
    private static partial Regex FileRegex();
}
