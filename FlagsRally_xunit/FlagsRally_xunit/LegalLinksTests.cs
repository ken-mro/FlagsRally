using System.Globalization;
using FlagsRally.Helpers;

namespace FlagsRallyTests.Helpers;

public class LegalLinksTests
{
    [Theory]
    [InlineData("ja-JP", "ja")]
    [InlineData("ja", "ja")]
    [InlineData("en-US", "en")]
    [InlineData("fr-FR", "en")]
    public void The_pages_follow_the_app_language(string culture, string language)
    {
        var c = new CultureInfo(culture);
        Assert.Equal($"https://ken-mro.github.io/FlagsRally/terms/{language}/", LegalLinks.TermsOfUse(c));
        Assert.Equal($"https://ken-mro.github.io/FlagsRally/privacy/{language}/", LegalLinks.PrivacyPolicy(c));
    }

    [Fact]
    public void Each_page_exists_in_the_repository()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "FlagsRally.csproj"))) root = root.Parent;
        Assert.NotNull(root);
        foreach (var doc in new[] { "terms", "privacy" })
            foreach (var lang in new[] { "en", "ja" })
                Assert.True(File.Exists(Path.Combine(root!.FullName, "docs", doc, lang, "index.html")), $"{doc}/{lang}");
    }
}
