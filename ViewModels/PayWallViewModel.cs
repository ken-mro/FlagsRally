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

    [RelayCommand]
    private void LoadOfferings()
    {
        Task.Run(async () =>
        {
            var loadedOfferings = await _revenueCatBilling.GetOfferings();
            LoadedOfferings = new ObservableCollection<OfferingDto>(loadedOfferings);

            MonthlySubscription = LoadedOfferings
                .SelectMany(x => x.AvailablePackages)
                .First(x => x.Identifier == DefaultPackageIdentifier.Monthly);

            YearlySubscription = LoadedOfferings
                .SelectMany(x => x.AvailablePackages)
                .First(x => x.Identifier == DefaultPackageIdentifier.Annually);
        });
    }

    [RelayCommand]
    private void BuyItem(PackageDto packageDto)
    {
        if (IsBusy) return;
        IsBusy = true;

        Task.Run(async () =>
        {
            var purchaseResult = await _revenueCatBilling.PurchaseProduct(packageDto);
            _settingsPreferences.SetIsSubscribed(purchaseResult.IsSuccess);
            IsBusy = false;
        });
    }
}
