using System.Xml.Linq;

namespace FlagsRallyTests.Views;

/// <summary>
/// The app's own wording must always be readable in full, in English and in Japanese,
/// and with the large text sizes of Android and iOS. Truncating a label hides the problem
/// when buttons are added beside it, so text must wrap instead (Japanese is often the longer one).
/// Only names that come from boards, countries or the user may be cut short.
/// </summary>
public class XamlTextTruncationTests
{
    /// <summary>
    /// The only texts a label may truncate: names from outside the app.
    /// A binding such as FilteredCountry.CountryName or ArrivalDateString is not here on purpose:
    /// it shows the app's own "All Countries" or a date, which must wrap.
    /// </summary>
    static readonly string[] ExternalNameBindings =
    [
        "{Binding Name}",
        "{Binding DisplayName}",
        "{Binding Title}",
        "{Binding CountryName}",
        "{Binding AdminAreaName}",
        "{Binding FolderTitle}",
    ];

    [Fact]
    public void Only_external_names_are_truncated()
    {
        var offenders =
            from file in XamlFiles()
            from element in file.Document.Descendants()
            where element.Name.LocalName.EndsWith("Label")
            let lineBreak = (string?)element.Attribute("LineBreakMode") ?? ""
            where lineBreak.EndsWith("Truncation") || element.Attribute("MaxLines") is not null
            let text = (string?)element.Attribute("Text") ?? ""
            where !ExternalNameBindings.Contains(text)
            select $"{file.Name}: {text}";

        Assert.Empty(offenders);
    }

    [Fact]
    public void App_strings_are_not_placeholders()
    {
        // A placeholder is a single line that cannot wrap; put the hint in a label instead.
        var offenders =
            from file in XamlFiles()
            from element in file.Document.Descendants()
            let placeholder = (string?)element.Attribute("Placeholder") ?? ""
            where placeholder.Contains("strings:AppResources.")
            select $"{file.Name}: {placeholder}";

        Assert.Empty(offenders);
    }

    static IEnumerable<(string Name, XDocument Document)> XamlFiles()
    {
        var root = RepositoryRoot();
        return Directory.EnumerateFiles(Path.Combine(root, "Views"), "*.xaml")
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "Controls"), "*.xaml"))
            .Select(path => (Path.GetFileName(path), XDocument.Load(path)));
    }

    static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FlagsRally.csproj")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("FlagsRally.csproj not found");
    }
}
