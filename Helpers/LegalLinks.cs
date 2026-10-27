using System.Globalization;

namespace FlagsRally.Helpers;

/// <summary>
/// The Terms of Use and the Privacy Policy on GitHub Pages (docs/terms, docs/privacy), in Japanese when
/// the app is in Japanese and in English otherwise.
/// </summary>
public static class LegalLinks
{
    const string Site = "https://ken-mro.github.io/FlagsRally/";

    public static string TermsOfUse(CultureInfo? culture = null) => $"{Site}terms/{Language(culture)}/";

    public static string PrivacyPolicy(CultureInfo? culture = null) => $"{Site}privacy/{Language(culture)}/";

    static string Language(CultureInfo? culture) =>
        (culture ?? CultureInfo.CurrentUICulture).TwoLetterISOLanguageName == "ja" ? "ja" : "en";
}
