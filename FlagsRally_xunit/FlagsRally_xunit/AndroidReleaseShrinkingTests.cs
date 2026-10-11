using System.Xml.Linq;

namespace FlagsRallyTests.Build;

/// <summary>
/// Release 1.1.20 crashed on launch: R8 (AndroidLinkTool=r8) removed
/// kotlinx.coroutines.android.AndroidDispatcherFactory, and RevenueCat's Purchases.configure
/// then failed with "Module with the Main dispatcher is missing". The class is only reached
/// through META-INF/services, and the .NET Android build passes neither those files nor the
/// keep rules inside the libraries' jars (META-INF/proguard) to R8; only .aar proguard.txt files.
/// So R8 may only be turned on together with our own rules that keep such classes.
/// </summary>
public class AndroidReleaseShrinkingTests
{
    // Classes the bound jars load through META-INF/services and keep with their own (ignored) rules.
    static readonly string[] ServiceLoaderClasses =
    [
        "kotlinx.coroutines.android.AndroidDispatcherFactory",
        "kotlinx.coroutines.android.AndroidExceptionPreHandler",
    ];

    [Fact]
    public void R8_IsOnlyUsedWithRulesKeepingServiceLoaderClasses()
    {
        var root = RepositoryRoot();
        var project = XDocument.Load(Path.Combine(root, "FlagsRally.csproj"));

        var usesR8 = project.Descendants()
            .Any(e => e.Name.LocalName == "AndroidLinkTool" && e.Value.Trim() == "r8");
        if (!usesR8) return;

        var rules = string.Join("\n", project.Descendants()
            .Where(e => e.Name.LocalName == "ProguardConfiguration")
            .Select(e => e.Attribute("Include")?.Value)
            .OfType<string>()
            .Select(path => File.ReadAllText(Path.Combine(root, path.Replace('\\', Path.DirectorySeparatorChar)))));

        foreach (var className in ServiceLoaderClasses)
        {
            Assert.True(rules.Contains($"-keep class {className}"),
                $"R8 is on but no ProguardConfiguration keeps {className}; the app crashes on launch without it.");
        }
    }

    static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FlagsRally.csproj")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("FlagsRally.csproj not found");
    }
}
