using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FlagsRally.ViewModels;

/// <summary>
/// The popup after the stamp of a new region: its flag (as the country's regional flags show it)
/// joining the country's card from Collections, whose progress moves on by one.
/// </summary>
public partial class CollectionAddedPopupViewModel(string regionName, string flagSource, string countryName, int visited, int total) : BaseViewModel
{
    public Popup? Popup { get; set; }

    public string RegionName { get; } = regionName;
    public string FlagSource { get; } = flagSource;
    public string CountryName { get; } = countryName;
    public int Visited { get; } = visited;
    public int Total { get; } = total;

    // The progress before and with the new region; the bar moves from one to the other
    public double ProgressBefore => Total == 0 ? 0 : Math.Max(Visited - 1, 0) / (double)Total;
    public double ProgressAfter => Total == 0 ? 0 : (double)Visited / Total;
    public string ProgressText => $"{Visited} / {Total}";

    // Set by the view once the flag has been pressed
    [ObservableProperty]
    bool _canClose;

    [RelayCommand]
    async Task ClosePopup()
    {
        if (CanClose && Popup is not null)
        {
            await Popup.CloseAsync();
        }
    }
}
