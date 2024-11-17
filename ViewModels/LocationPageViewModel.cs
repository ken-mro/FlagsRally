using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlagsRally.Helpers;
using FlagsRally.Models;
using FlagsRally.Models.CustomBoard;
using FlagsRally.Repository;
using FlagsRally.Resources;
using FlagsRally.Services;
using FlagsRally.Views;
using Maui.GoogleMaps;
using Maui.RevenueCat.InAppBilling.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using Map = Maui.GoogleMaps.Map;
using MapSpan = Maui.GoogleMaps.MapSpan;

namespace FlagsRally.ViewModels;

public partial class LocationPageViewModel : BaseViewModel
{
    private const double DEFAULT_LATITUDE = 46.22667333333333;
    private const double DEFAULT_LONGITUDE = 6.140291666666666;
    private readonly IArrivalLocationDataRepository _arrivalLocationRepository;
    private readonly CustomGeolocation _customGeolocation;
    private CancellationTokenSource? _cancelTokenSource;
    private bool _isCheckingLocation;
    private IRevenueCatBilling _revenueCat;
    private SettingsPreferences _settingsPreferences;
    private Map? _arrivalMap;
    private CustomBoardService _customBoardService;

    public Map? ArrivalMap
    {
        get => _arrivalMap;
        set
        {
            SetProperty(ref _arrivalMap, value);
            _arrivalMap!.MyLocationEnabled = true;
            _arrivalMap.UiSettings.MyLocationButtonEnabled = true;
            _arrivalMap.UiSettings.CompassEnabled = true;
            _arrivalMap.UiSettings.ScrollGesturesEnabled = true;
            _ = Init();
        }
    }

    public LocationPageViewModel(IArrivalLocationDataRepository arrivalLocationRepository, CustomGeolocation customGeolocation, IRevenueCatBilling revenueCat, SettingsPreferences settingsPreferences, CustomBoardService customBoardService)
    {
        _arrivalLocationRepository = arrivalLocationRepository;
        _customGeolocation = customGeolocation;
        _revenueCat = revenueCat;
        _settingsPreferences = settingsPreferences;
        _customBoardService = customBoardService;
    }

    private async Task Init()
    {
        try
        {
            IsBusy = true;
            _isCheckingLocation = true;
            PinFilterList = new (CustomBoardPinFilterItem.CreateFilterList());

            var arrivalLocationPins = await _arrivalLocationRepository.GetArrivalLocationPinsAsync();
            var arrivalLocationPin = arrivalLocationPins.FirstOrDefault();
            var tag = arrivalLocationPin?.Tag as MapPinTag;
            if (tag is not null)
            {
                PinFilterList.Add(new CustomBoardPinFilterItem(tag.PinId, AppResources.ArrivalLocation));
            }

            foreach (var pin in arrivalLocationPins)
            {
                ArrivalMap?.Pins.Add(pin);
            }

            //Temp code. Get data locally.
            var info = System.Reflection.Assembly.GetExecutingAssembly().GetName();
            using var stream1 = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream($"{info.Name}.Resources.Sample.manhole_card_23_1.json");
            using var stream2 = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream($"{info.Name}.Resources.Sample.manhole_card_23_2.json");
            using var stream3 = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream($"{info.Name}.Resources.Sample.manhole_card_23_3.json");
            var customLocationList = GetResources([stream1!, stream2!, stream3!]);
            AddPinsToMap(customLocationList);

            Location location = await Geolocation.Default.GetLastKnownLocationAsync() 
                                ?? new Location(DEFAULT_LATITUDE, DEFAULT_LONGITUDE);
            var position = new Position(location.Latitude, location.Longitude);
            ArrivalMap?.MoveToRegion(MapSpan.FromCenterAndRadius(position, Distance.FromMeters(5000)));
        }
        catch (Exception ex)
        {
            // Unable to get location
#if DEBUG
            Console.WriteLine(ex.Message);
#endif
            var position = new Position(DEFAULT_LATITUDE, DEFAULT_LONGITUDE);
            ArrivalMap?.MoveToRegion(MapSpan.FromCenterAndRadius(position, Distance.FromMeters(5000)));
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

            if (ArrivalMap?.Pins.Count >= 5 && !_settingsPreferences.IsApiKeySet())
            {
                await TryToOfferSubscription();
                if (!_settingsPreferences.GetIsSubscribed()) return;
            }

            GeolocationRequest request = new (GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
#if IOS
            request.RequestFullAccuracy = true;
#endif

            _cancelTokenSource = new CancellationTokenSource();
            var location = await Geolocation.Default.GetLocationAsync(request, _cancelTokenSource.Token);
            if (location is null)
                throw new Exception($"{AppResources.UnableToGetLocation}");

            string languageCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var arrivalLocationData = await _customGeolocation.GetArrivalLocationAsync(DateTime.Now, location, languageCode);

            if (arrivalLocationData is null)
                throw new Exception($"{AppResources.UnableToGetLocationData}");

            var result = await Shell.Current.DisplayAlert($"{AppResources.Confirmation}", $"{AppResources.IsTheFollowingYourLocatoin}\n\n" +
                                                            $"{arrivalLocationData}", $"{AppResources.Yes}", $"{AppResources.No}");
            if (result)
            {
                var id = await _arrivalLocationRepository.Save(arrivalLocationData);
                _settingsPreferences.SetLatestCountry(arrivalLocationData.CountryCode);

                ArrivalLocationPin arrivalLocationPin = new(id, arrivalLocationData.ArrivalDate, location);
                ArrivalMap?.Pins.Add(arrivalLocationPin);
            }
        }
        catch (FeatureNotSupportedException ex)
        {
            // Handle not supported on device exception
            await Shell.Current.DisplayAlert($"{AppResources.Error}", $"{ex.Message}\nNot supported on device.", "OK");
        }
        catch (FeatureNotEnabledException ex)
        {
            // Handle not enabled on device exception
            await Shell.Current.DisplayAlert($"{AppResources.Error}", $"{ex.Message}\nNot enabled on device.", "OK");
        }
        catch (PermissionException ex)
        {
            // Handle permission exception
            await Shell.Current.DisplayAlert($"{AppResources.Error}", $"{ex.Message}\nSomething wrong with the permission", "OK");
        }
        catch (Exception ex)
        {
            // Unable to get location
            // Todo:add logger
            await Shell.Current.DisplayAlert($"{AppResources.Error}", $"{ex.Message}\n{AppResources.PleaseTryAgain}", "OK");
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

    [ObservableProperty]
    ObservableCollection<CustomBoardPinFilterItem> _pinFilterList = default!;

    CustomBoardPinFilterItem _filteredPinItem = default!;

    public CustomBoardPinFilterItem FilteredPinItem
    {
        get => _filteredPinItem;
        set
        {
            SetProperty(ref _filteredPinItem, value);
            UpdatePinsVisibility();
        }
    }

    private void UpdatePinsVisibility()
    {
        if (ArrivalMap?.Pins is null) return;
        foreach (var pin in ArrivalMap.Pins)
        {
            Console.WriteLine((pin.Tag as MapPinTag)?.PinId);
            if (FilteredPinItem.IsAll)
            {
                pin.IsVisible = true;
                continue;
            }

            if (FilteredPinItem.Id ==(pin.Tag as MapPinTag)?.PinId)
            {
                pin.IsVisible = true;
            }
            else
            {
                pin.IsVisible = false;
            }
        }
    }
    private async Task TryToOfferSubscription()
    {
        var customerInfo = await _revenueCat.GetCustomerInfo();
        var isSubscribed = customerInfo?.ActiveSubscriptions?.Count > 0;
        _settingsPreferences.SetIsSubscribed(isSubscribed);

        if (_settingsPreferences.GetIsSubscribed()) return;
        await Shell.Current.CurrentPage.ShowPopupAsync(new PayWallView(new PayWallViewModel(_revenueCat, _settingsPreferences)));
    }

    private void AddPinsToMap(IEnumerable<CustomLocationPin> customLocationList)
    {
        foreach (var pin in customLocationList)
        {
            ArrivalMap?.Pins.Add(pin);
        }
    }

    private IEnumerable<CustomLocationPin> GetResources(Stream[] streamList)
    {
        var sourceList = new List<CustomLocationPin>();
        foreach (var stream in streamList)
        {
            if (stream == null) continue;
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var customBoardJson = JsonSerializer.Deserialize<CustomBoardJson>(json) ?? new();
            
            (var customBoard, var pins) = _customBoardService.GetCustomLocationPins(customBoardJson);
            sourceList.AddRange(pins);
            PinFilterList.Add(new CustomBoardPinFilterItem(customBoard));
        }

        return sourceList.AsEnumerable();
    }
}