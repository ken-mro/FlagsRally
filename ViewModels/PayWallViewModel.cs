using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlagsRally.Resources;
using Maui.RevenueCat.InAppBilling.Models;
using Maui.RevenueCat.InAppBilling.Services;
using System.Collections.ObjectModel;

namespace FlagsRally.ViewModels;

public partial class PayWallViewModel : BaseViewModel
{
    private readonly IRevenueCatBilling _revenueCatBilling;

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

    public PayWallViewModel(IRevenueCatBilling revenueCatBilling)
    {
        _revenueCatBilling = revenueCatBilling;
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
        //var originalText = button.Text;
        //button.Text = "Purchasing...";
        //button.IsEnabled = false;

        Task.Run(async () =>
        {
            //await _revenueCatBilling.PurchaseProduct(buttonText);
            //if (button == BtnMonthly)
            //{
            //    await _revenueCatBilling.PurchaseProduct(_monthlySubscription);
            //}
            //else if (button == BtnYearly)
            //{
            //    await _revenueCatBilling.PurchaseProduct(_yearlySubscription);
            //}
            //else if (button == BtnConsumable1)
            //{
            //    await _revenueCatBilling.PurchaseProduct(_consumable1);
            //}
            //else if (button == BtnConsumable2)
            //{
            //    await _revenueCatBilling.PurchaseProduct(_consumable2);
            //}

            //button.Dispatcher.Dispatch(() =>
            //{
            //    button.Text = originalText;
            //    button.IsEnabled = true;
            //});
            IsBusy = false;
        });
    }

    //private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    //{
    //    if (PropertyChanged != null)
    //    {
    //        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    //    }
    //}
}
