using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlagsRally.Models;
using FlagsRally.Repository;
using FlagsRally.Resources;
using FlagsRally.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Maps;
using Plugin.InAppBilling;
using System.Collections.ObjectModel;
using System.Globalization;

namespace FlagsRally.ViewModels;

public partial class LocationPageViewModel : BaseViewModel
{
    private readonly SettingsPreferences _settingsPreferences;
    private readonly IArrivalLocationDataRepository _arrivalLocationRepository;
    private readonly CustomGeolocation _customGeolocation;
    private CancellationTokenSource _cancelTokenSource;
    private bool _isCheckingLocation;
    public Microsoft.Maui.Controls.Maps.Map ArrivalMap;

    [ObservableProperty]
    ObservableCollection<ArrivalLocationPin> _positions;

    public LocationPageViewModel(IArrivalLocationDataRepository arrivalLocationRepository, CustomGeolocation customGeolocation, SettingsPreferences settingsPreferences)
    {
        _arrivalLocationRepository = arrivalLocationRepository;
        _customGeolocation = customGeolocation;
        _settingsPreferences = settingsPreferences;
        _ = init();
    }

    private async Task init()
    {
        var arrivalLocationPins = await _arrivalLocationRepository.GetArrivalLocationPinsAsync();
        Positions = new ObservableCollection<ArrivalLocationPin>(arrivalLocationPins);
        try
        {
            IsBusy = true;
            _isCheckingLocation = true;

            Location location = await Geolocation.Default.GetLastKnownLocationAsync();
            MapSpan mapSpan = new MapSpan(location, 0.01, 0.01);
            ArrivalMap.MoveToRegion(mapSpan);
        }
        catch (Exception ex)
        {
            // Unable to get location
            Location location = new Location(46.22667333333333, 6.140291666666666);
            MapSpan mapSpan = new MapSpan(location, 0.01, 0.01);
            ArrivalMap.MoveToRegion(mapSpan);

        }
        finally
        {
            IsBusy = false;
            _isCheckingLocation = false;
        }
    }

    [RelayCommand]
    public async Task GetCurrentLocationAsync()
    {
        if (IsBusy || _isCheckingLocation)
            return;
        try
        {
            IsBusy = true;
            _isCheckingLocation = true;

            //var isSubscribed = await MakePurchase();
            //if (!isSubscribed) return;

            GeolocationRequest request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
            var cancelTokenSource = new CancellationTokenSource();
            Location location = await Geolocation.Default.GetLocationAsync(request, cancelTokenSource.Token);
            var datetime = DateTime.Now;

            string languageCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var arrivalLocationData = await _customGeolocation.GetArrivalLocationAsync(datetime, location, languageCode);

            if (arrivalLocationData == null)
                throw new Exception($"{AppResources.UnableToGetLocation}");

            var result = await Shell.Current.DisplayAlert($"{AppResources.Confirmation}", $"{AppResources.IsTheFollowingYourLocatoin}\n\n" +
                                                            $"{arrivalLocationData}", $"{AppResources.Yes}", $"{AppResources.No}");
            if (result)
            {
                var id = await _arrivalLocationRepository.Save(arrivalLocationData);
                ArrivalLocationPin arrivalLocationPins = new()
                {
                    Id = id,
                    ArrivalDate = arrivalLocationData.ArrivalDate,
                    PinLocation = location
                };
                Positions.Add(arrivalLocationPins);
            }
        }
        // Catch one of the following exceptions:
        //   FeatureNotSupportedException
        //   FeatureNotEnabledException
        //   PermissionException
        catch (Exception ex)
        {
            // Unable to get location
            // Todo:add logger
            await Shell.Current.DisplayAlert($"{AppResources.Error}", $"{AppResources.UnableToGetLocation}\n{AppResources.PleaseTryAgain}", "OK");
        }
        finally
        {
            IsBusy = false;
            _isCheckingLocation = false;
        }
    }

    public void CancelRequest()
    {
        if (_isCheckingLocation && _cancelTokenSource != null && _cancelTokenSource.IsCancellationRequested == false)
            _cancelTokenSource.Cancel();
    }

    async Task PurchaseSubscription()
    {
        try
        {

            // check internet first with Essentials
            if (Connectivity.NetworkAccess != NetworkAccess.Internet)
                return;

            // connect to the app store api
            var connected = await CrossInAppBilling.Current.ConnectAsync();
            if (!connected)
                return;


            var productIdSub = "mysubscriptionid";

            //try to make purchase, this will return a purchase, empty, or throw an exception
            var purchase = await CrossInAppBilling.Current.PurchaseAsync(productIdSub, ItemType.Subscription);

            if (purchase == null)
            {
                //nothing was purchased
                return;
            }

            if (purchase.State == PurchaseState.Purchased)
            {
                _settingsPreferences.SubExpirationDate = DateTime.UtcNow.AddMonths(1).AddDays(5);
                _settingsPreferences.HasPurchasedSub = true;
                _settingsPreferences.CheckSubStatus = true;

                // Update UI if necessary if they have 

                try
                {
                    // It is required to acknowledge the purchase, else it will be refunded
                    if (DeviceInfo.Platform == DevicePlatform.Android)
                        await CrossInAppBilling.Current.FinalizePurchaseAsync(purchase.PurchaseToken);
                }
                catch (Exception ex)
                {
                    //Logger.AppendLine("Unable to acknowledge purcahse: " + ex);
                }
            }
            else
            {
                throw new InAppBillingPurchaseException(PurchaseError.GeneralError);
            }
        }
        catch (InAppBillingPurchaseException purchaseEx)
        {
            // Handle all the different error codes that can occure and do a pop up
        }
        catch (Exception ex)
        {
            // Handle a generic exception as something really went wrong
        }
        finally
        {
            await CrossInAppBilling.Current.DisconnectAsync();
        }
    }

    //private async Task<bool> MakePurchase()
    //{

    //    try
    //    {
    //        // check internet first with Essentials
    //        if (Connectivity.NetworkAccess != NetworkAccess.Internet)
    //            return false;

    //        if (!CrossInAppBilling.IsSupported)
    //            return false;

    //        var billing = CrossInAppBilling.Current;
    //        var connected = await billing.ConnectAsync();
    //        if (!connected)
    //        {
    //            //Couldn't connect to billing, could be offline
    //            return false;
    //        }

    //        //check purchases
    //        var purchases = await billing.GetPurchasesAsync(ItemType.InAppPurchase);
    //        if (purchases?.Any(p => p.ProductId == "remove_ads") == true)
    //        {
    //            //Already purchased
    //            return true;
    //        }

    //        //check for available in-app purchases
    //        var items = await billing.GetProductInfoAsync(ItemType.InAppPurchase, "remove_ads");
    //        if (items?.Any() != true)
    //        {
    //            //No Items available for purchase
    //            return false;
    //        }

    //        //make purchase
    //        var purchase = await billing.PurchaseAsync(items[0].ProductId, ItemType.InAppPurchase, "apppayload");
    //        return purchase != null;
    //    }
    //    catch (Exception ex)
    //    {
    //        //Something has gone wrong
    //        return false;
    //    }
    //    finally
    //    {
    //        await CrossInAppBilling.Current.DisconnectAsync();
    //        CrossInAppBilling.Dispose();
    //    }
    //}
}