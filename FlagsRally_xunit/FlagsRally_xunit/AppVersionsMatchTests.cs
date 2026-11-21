using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace FlagsRallyTests.Build;

/// <summary>
/// Android and iOS go out under the same version. iOS 1.4.3 (build 28) was uploaded from a
/// release branch whose version commit never reached master, so master still said 1.4.2 (27) and
/// the next bump would have reused build 28. Both release skills put the version commits on master;
/// this test fails as soon as the two numbers drift apart again.
/// </summary>
public class AppVersionsMatchTests
{
    [Fact]
    public void AndroidAndIosVersions_AreTheSame()
    {
        var root = RepositoryRoot();
        Assert.Equal(AndroidVersion(root), IosVersion(root));
    }

    static string AndroidVersion(string root)
    {
        var manifest = XDocument.Load(Path.Combine(root, "Platforms", "Android", "AndroidManifest.xml"));
        XNamespace android = "http://schemas.android.com/apk/res/android";
        return manifest.Root?.Attribute(android + "versionName")?.Value
            ?? throw new InvalidDataException("android:versionName not found");
    }

    static string IosVersion(string root)
    {
        var plist = File.ReadAllText(Path.Combine(root, "Platforms", "iOS", "Info.plist"));
        var match = Regex.Match(plist, @"<key>CFBundleShortVersionString</key>\s*<string>([^<]*)</string>");
        return match.Success ? match.Groups[1].Value.Trim()
            : throw new InvalidDataException("CFBundleShortVersionString not found");
    }

    static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FlagsRally.csproj")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("FlagsRally.csproj not found");
    }
}
