namespace FlagsRally.Helpers;

public static class AppVersionText
{
    /// <summary>
    /// The installed app's version for people: "1.3.0 (24)". AppInfo reads it from the package on each
    /// platform (Android's versionName and versionCode, iOS's CFBundleShortVersionString and CFBundleVersion).
    /// </summary>
    public static string Format(string version, string build) =>
        string.IsNullOrWhiteSpace(build) || build == version ? version : $"{version} ({build})";

    public static string Current => Format(AppInfo.Current.VersionString, AppInfo.Current.BuildString);
}
