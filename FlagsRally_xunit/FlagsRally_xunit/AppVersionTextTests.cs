using FlagsRally.Helpers;

namespace FlagsRallyTests.Helpers;

public class AppVersionTextTests
{
    [Theory]
    [InlineData("1.3.0", "24", "1.3.0 (24)")]       // iOS: CFBundleShortVersionString and CFBundleVersion
    [InlineData("1.3.0", "10300", "1.3.0 (10300)")] // Android: versionName and versionCode
    [InlineData("1.3.0", "", "1.3.0")]
    [InlineData("1.3.0", "1.3.0", "1.3.0")]
    public void The_version_is_followed_by_its_build(string version, string build, string text)
    {
        Assert.Equal(text, AppVersionText.Format(version, build));
    }
}
