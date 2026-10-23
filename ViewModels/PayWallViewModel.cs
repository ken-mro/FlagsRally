using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlagsRally.Repository;
using FlagsRally.Resources;
using Maui.RevenueCat.InAppBilling.Models;
using Maui.RevenueCat.InAppBilling.Services;
using System.Collections.ObjectModel;

namespace FlagsRally.ViewModels;

public partial class PayWallViewModel : BaseViewModel
{
    private readonly IRevenueCatBilling _revenueCatBilling;
    private readonly SettingsPreferences _settingsPreferences;
    public Popup Popup = new();

    //RC data
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AreOfferingsLoaded))]
    private ObservableCollection<OfferingDto> _loadedOfferings = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthlyButtonText))]
    private PackageDto _monthlySubscription = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(YearlyButtonText))]
    private PackageDto _yearlySubscription = new();

    //UI data
    public bool AreOfferingsLoaded => LoadedOfferings.Any();
    public string MonthlyButtonText => $"{AppResources.MonthlySubFor} {MonthlySubscription.Product.Pricing.PriceLocalized}";
    public string YearlyButtonText => $"{AppResources.YearlySubFor} {YearlySubscription.Product.Pricing.PriceLocalized}";

    public PayWallViewModel(IRevenueCatBilling revenueCatBilling, SettingsPreferences settingsPreferences)
    {
        _revenueCatBilling = revenueCatBilling;
        _settingsPreferences = settingsPreferences;
        Title = "Pay Wall";
    }

    // Both commands run on the main thread: what they set is bound to the popup, saving the
    // subscription updates other pages, and closing the popup dismisses it. iOS hangs when any
    // of these happens on a background thread, as they did from Task.Run before.
    [RelayCommand]
    private async Task LoadOfferingsAsync()
    {
        var offeringsResult = await _revenueCatBilling.GetOfferings();
        if (offeringsResult.IsError || offeringsResult.Value is null) return;

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            LoadedOfferings = new ObservableCollection<OfferingDto>(offeringsResult.Value);

            MonthlySubscription = LoadedOfferings
                .SelectMany(x => x.AvailablePackages)
                .First(x => x.Identifier == DefaultPackageIdentifier.Monthly);

            YearlySubscription = LoadedOfferings
                .SelectMany(x => x.AvailablePackages)
                .First(x => x.Identifier == DefaultPackageIdentifier.Annually);
        });
    }

    [RelayCommand]
    private async Task BuyItemAsync(PackageDto packageDto)
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var purchaseResult = await _revenueCatBilling.PurchaseProduct(packageDto);
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                _settingsPreferences.SetIsSubscribed(purchaseResult.IsSuccess);
                if (purchaseResult.IsSuccess)
                {
                    await Popup.CloseAsync();
                }
            });
        }
        finally
        {
            await MainThread.InvokeOnMainThreadAsync(() => IsBusy = false);
        }
    }
}
