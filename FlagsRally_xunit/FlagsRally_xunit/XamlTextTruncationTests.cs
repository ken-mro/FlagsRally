using System.Xml.Linq;

namespace FlagsRallyTests.Views;

/// <summary>
/// The app's own wording must always be readable in full, in English and in Japanese.
/// Truncating a label hides the problem when buttons are added beside it, so a label
/// showing an AppResources string must wrap instead (Japanese is often the longer one).
/// Names that come from boards or the user may still be truncated.
/// </summary>
public class XamlTextTruncationTests
{
    [Fact]
    public void App_strings_are_never_truncated()
    {
        var root = RepositoryRoot();
        var offenders =
            from path in Directory.EnumerateFiles(Path.Combine(root, "Views"), "*.xaml")
                .Concat(Directory.EnumerateFiles(Path.Combine(root, "Controls"), "*.xaml"))
            from element in XDocument.Load(path).Descendants()
            where element.Name.LocalName.EndsWith("Label")
            let text = (string?)element.Attribute("Text") ?? ""
            where text.Contains("strings:AppResources.")
            let lineBreak = (string?)element.Attribute("LineBreakMode") ?? ""
            where lineBreak.EndsWith("Truncation") || element.Attribute("MaxLines") is not null
            select $"{Path.GetFileName(path)}: {text}";

        Assert.Empty(offenders);
    }

    static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FlagsRally.csproj")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("FlagsRally.csproj not found");
    }
}
