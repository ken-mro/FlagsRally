using CommunityToolkit.Mvvm.ComponentModel;
using FlagsRally.Models;
using System.Globalization;

namespace FlagsRally.Repository;

public partial class SettingsPreferences : ObservableObject
{
    private IPreferences _defaultPreferences;
    private string _selectedCountryOrRegionKey = "SelectedCountryOrRegion";
    private string _isSubscribed = "IsSubscribed";
    private string _latestCountry = "LatestCountry";
    private string _apiKey = "ApiKey";
    private string _customBoardSort = "CustomBoardSort";
    private string _pinStyle = "PinStyle";
    private string _regionalFlagsExpanded = "RegionalFlagsExpanded";
    private string _hasSeenOnboarding = "HasSeenOnboarding";
    private string _hasSeenLongPressHint = "HasSeenLongPressHint";

    public SettingsPreferences(IPreferences defaultPreferences)
    {
        _defaultPreferences = defaultPreferences;
    }

    public void SetCountryOfResidence(string twoLetterISORegionName)
    {
        if (twoLetterISORegionName.Length != 2) throw new ArgumentException("Two-letter ISO region name must be 2 characters long");

        if (!twoLetterISORegionName.All(Char.IsLetter)) throw new ArgumentException("Two-letter ISO region name only has letters");

        _defaultPreferences.Set(_selectedCountryOrRegionKey, twoLetterISORegionName);
        OnPropertyChanged();
    }

    public string GetCountryOfResidence()
    {
        var stored = _defaultPreferences.Get(_selectedCountryOrRegionKey, string.Empty);
        return IsCountryCode(stored) ? stored : DefaultCountryOfResidence();
    }

    /// <summary>
    /// The device's region until one is chosen in Settings. With only a language set for the app (iOS
    /// lets each app have its own), .NET's current region can be the invariant one ("IV"), which is
    /// no country: the passport cover then did not load and Settings closed the app. The device's own
    /// region setting comes next, and the United States last.
    /// </summary>
    static string DefaultCountryOfResidence() => PickCountryCode(
        RegionInfo.CurrentRegion.TwoLetterISORegionName,
#if IOS || MACCATALYST
        Foundation.NSLocale.CurrentLocale.CountryCode,
#elif ANDROID
        Java.Util.Locale.Default.Country,
#endif
        "US");

    /// <summary>
    /// The first of the codes that is a real two-letter country code, in upper case.
    /// </summary>
    public static string PickCountryCode(params string?[] candidates) =>
        candidates.Where(IsCountryCode).Select(c => c!.ToUpperInvariant()).FirstOrDefault() ?? "US";

    // "IV" is .NET's invariant region and "ZZ" / "001" stand for unknown or the world.
    static bool IsCountryCode(string? code) =>
        code is { Length: 2 } && code.All(char.IsAsciiLetter)
        && !code.Equals("IV", StringComparison.OrdinalIgnoreCase) && !code.Equals("ZZ", StringComparison.OrdinalIgnoreCase);

    public void SetIsSubscribed(bool isSubscribed)
    {
        _defaultPreferences.Set(_isSubscribed, isSubscribed.ToString());
        OnPropertyChanged();
    }

    public bool GetIsSubscribed()
    {
        return bool.Parse(_defaultPreferences.Get(_isSubscribed, false.ToString()));
    }

    public string GetLatestCountry()
    {
        return _defaultPreferences.Get(_latestCountry, GetCountryOfResidence());
    }

    public void SetLatestCountry(string countryCode)
    {
        _defaultPreferences.Set(_latestCountry, countryCode);
    }

    public bool IsApiKeySet()
    {
        return !string.IsNullOrEmpty(GetApiKey());
    }

    public string GetApiKey()
    {
        return _defaultPreferences.Get(_apiKey, string.Empty);
    }

    public void SetApiKey(string apiKey)
    {
        _defaultPreferences.Set(_apiKey, apiKey);
    }

    public string GetCustomBoardSort(string defaultValue)
    {
        return _defaultPreferences.Get(_customBoardSort, defaultValue);
    }

    public void SetCustomBoardSort(string sort)
    {
        _defaultPreferences.Set(_customBoardSort, sort);
    }

    public PinStyle GetPinStyle()
    {
        return Enum.TryParse<PinStyle>(_defaultPreferences.Get(_pinStyle, nameof(PinStyle.Classic)), out var style) ? style : PinStyle.Classic;
    }

    public void SetPinStyle(PinStyle style)
    {
        _defaultPreferences.Set(_pinStyle, style.ToString());
    }

    public bool GetRegionalFlagsExpanded()
    {
        return bool.Parse(_defaultPreferences.Get(_regionalFlagsExpanded, true.ToString()));
    }

    public void SetRegionalFlagsExpanded(bool isExpanded)
    {
        _defaultPreferences.Set(_regionalFlagsExpanded, isExpanded.ToString());
    }

    // The how-to pages shown on the first launch (and again from Settings).
    public bool GetHasSeenOnboarding() => _defaultPreferences.Get(_hasSeenOnboarding, false);

    public void SetHasSeenOnboarding() => _defaultPreferences.Set(_hasSeenOnboarding, true);

    // The one-time hint on the map that a long press puts a stamp spot.
    public bool GetHasSeenLongPressHint() => _defaultPreferences.Get(_hasSeenLongPressHint, false);

    public void SetHasSeenLongPressHint() => _defaultPreferences.Set(_hasSeenLongPressHint, true);
}
