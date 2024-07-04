using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;

namespace FlagsRally.Repository;

public partial class SettingsPreferences : ObservableObject
{
    private IPreferences _defaultPreferences;
    private string _selectedCountryOrRegionKey = "SelectedCountryOrRegion";
    private string _isSubscribed = "IsSubscribed";

    public SettingsPreferences(IPreferences defaultPreferences)
    {
        _defaultPreferences = defaultPreferences;
    }

    public void SetCountryOrRegion(string twoLetterISORegionName)
    {
        if (twoLetterISORegionName.Length != 2) throw new ArgumentException("Two-letter ISO region name must be 2 characters long");

        if (!twoLetterISORegionName.All(Char.IsLetter)) throw new ArgumentException("Two-letter ISO region name only has letters");

        _defaultPreferences.Set(_selectedCountryOrRegionKey, twoLetterISORegionName);
        OnPropertyChanged();
    }

    public string GetCountryOrRegion()
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
}
