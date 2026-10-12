using System.Text;

namespace FlagsRallyTests.Build;

/// <summary>
/// Source files must be UTF-8. The C# compiler reads a file without a BOM as UTF-8 and, only on
/// Windows, falls back to the system code page when that fails. Two test files saved in
/// Shift_JIS therefore passed on Japanese Windows and failed everywhere else, with garbled
/// Japanese expected values.
/// </summary>
public class SourceEncodingTests
{
    static readonly string[] Extensions = [".cs", ".xaml", ".resx", ".csproj", ".json"];

    [Fact]
    public void Source_files_are_utf8()
    {
        var strictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        var root = RepositoryRoot();

        var offenders = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Where(path => !IsBuildOutput(Path.GetRelativePath(root, path)))
            .Where(path =>
            {
                try { strictUtf8.GetString(File.ReadAllBytes(path)); return false; }
                catch (DecoderFallbackException) { return true; }
            })
            .Select(path => Path.GetRelativePath(root, path));

        Assert.Empty(offenders);
    }

    static bool IsBuildOutput(string relativePath) =>
        relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(part => part is "bin" or "obj" or ".git" or "node_modules");

    static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FlagsRally.csproj")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("FlagsRally.csproj not found");
    }
}
