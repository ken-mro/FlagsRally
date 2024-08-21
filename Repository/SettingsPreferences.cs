using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;

namespace FlagsRally.Repository;

public partial class SettingsPreferences : ObservableObject
{
    private IPreferences _defaultPreferences;
    private string _selectedCountryOrRegionKey = "SelectedCountryOrRegion";
    private string _isSubscribed = "IsSubscribed";
    private string _latestCountry = "LatestCountry";
    private string _apiKey = "ApiKey";

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
        var twoLetterISORegionName = RegionInfo.CurrentRegion.TwoLetterISORegionName;
        return _defaultPreferences.Get(_selectedCountryOrRegionKey, twoLetterISORegionName);
    }

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
}
