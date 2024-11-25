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
using Map = Maui.GoogleMaps.Map;
using MapSpan = Maui.GoogleMaps.MapSpan;

namespace FlagsRally.ViewModels;

public partial class LocationPageViewModel : BaseViewModel
{
    private const double DEFAULT_LATITUDE = 46.22667333333333;
    private const double DEFAULT_LONGITUDE = 6.140291666666666;
    private readonly IArrivalLocationDataRepository _arrivalLocationRepository;
    private readonly ICustomBoardRepository _customBoardRepository;
    private readonly ICustomLocationDataRepository _customLocationDataRepository;
    private readonly CustomGeolocation _customGeolocation;
    private readonly AppShell _appShell;
    private CancellationTokenSource? _cancelTokenSource;
    private bool _isCheckingLocation;
    private IRevenueCatBilling _revenueCat;
    private SettingsPreferences _settingsPreferences;
    private Map? _arrivalMap;
    private CustomBoardService _customBoardService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectesCustomLocationPin))]
    Pin? _selectedPin;

    public bool SelectesCustomLocationPin => (SelectedPin?.Tag as MapPinTag)?.IsCustomLocation ?? false;

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
            _arrivalMap.UiSettings.MapToolbarEnabled = true;
            _ = Init();
        }
    }

    public LocationPageViewModel(IArrivalLocationDataRepository arrivalLocationRepository, CustomGeolocation customGeolocation, IRevenueCatBilling revenueCat, SettingsPreferences settingsPreferences, CustomBoardService customBoardService, ICustomBoardRepository customBoardRepository, ICustomLocationDataRepository customLocationDataRepository, AppShell appShell)
    {
        _appShell = appShell;
        _arrivalLocationRepository = arrivalLocationRepository;
        _customBoardRepository = customBoardRepository;
        _customLocationDataRepository = customLocationDataRepository;
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

            await InitializeMapPins();

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

            if (SelectesCustomLocationPin)
            {
                await CheckInCustomLocation();
                return;
            }

            if (ArrivalMap?.Pins.Count >= 5 && !_settingsPreferences.IsApiKeySet())
            {
                await TryToOfferSubscription();
                if (!_settingsPreferences.GetIsSubscribed()) return;
            }

            var currentLocation = await GetCurrentLocation();

            string languageCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var arrivalLocationData = await _customGeolocation.GetArrivalLocationAsync(DateTime.Now, currentLocation, languageCode);

            if (arrivalLocationData is null)
                throw new Exception($"{AppResources.UnableToGetLocationData}");

            var result = await Shell.Current.DisplayAlert($"{AppResources.Confirmation}", $"{AppResources.IsTheFollowingYourLocatoin}\n\n" +
                                                            $"{arrivalLocationData}", $"{AppResources.Yes}", $"{AppResources.No}");
            if (result)
            {
                var id = await _arrivalLocationRepository.Save(arrivalLocationData);
                _settingsPreferences.SetLatestCountry(arrivalLocationData.CountryCode);

                ArrivalLocationPin arrivalLocationPin = new(arrivalLocationData.ArrivalDate, currentLocation);
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

    private async Task CheckInCustomLocation()
    {
        var pinPosition = SelectedPin!.Position;
        var pinLocation = new Location(pinPosition.Latitude, pinPosition.Longitude);
        var currentLocation = await GetCurrentLocation();

        var distance = pinLocation.CalculateDistance(currentLocation, DistanceUnits.Kilometers);
        var isNear = distance <= 0.05;
        if (!isNear)
        {
            await Shell.Current.DisplayAlert($"{AppResources.Error}", $"{AppResources.YouAreNotNearTheLocation}", "OK");
            return;
        }

        var tag = (MapPinTag)SelectedPin.Tag;
        var isCustomLocation = !string.IsNullOrEmpty(tag.CustomLocationKey);

        if (!isCustomLocation) return;

        _customLocationDataRepository?.UpdateCustomLocation(tag.CustomLocationKey, DateTime.Now);

        SelectedPin.Icon = CustomLocationPin.SetIcon(true);
    }

    private async Task<Location> GetCurrentLocation()
    {
        GeolocationRequest request = new(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
#if IOS
            request.RequestFullAccuracy = true;
#endif

        _cancelTokenSource = new CancellationTokenSource();
        var location = await Geolocation.Default.GetLocationAsync(request, _cancelTokenSource.Token);
        if (location is null)
            throw new Exception($"{AppResources.UnableToGetLocation}");

        return location;
    }

    [RelayCommand]
    public async Task AddCustomBoardJsonAsync()
    {
        if (IsBusy || _isCheckingLocation)
            return;
        try
        {
            IsBusy = true;
            var pickedFile = await FilePicker.PickAsync();
            if (pickedFile is null) return;
            using var stram = await pickedFile.OpenReadAsync();
            (var customBoard, var pins) = await _customBoardService.SaveBoardAndLocations(stram);

            if (!_appShell.CustomBoardPage.IsVisible)
            {
                _appShell.CustomBoardPage.IsVisible = true;
            }

            var filterItem = new CustomBoardPinFilterItem(customBoard);
            if (!PinFilterList.Where(f => f.Name.Equals(filterItem.Name)).Any())
            {
                PinFilterList.Add(filterItem);
                FilteredPinItem = filterItem;
            }
            else
            {
                FilteredPinItem = PinFilterList.Where(f => f.Name.Equals(filterItem.Name)).FirstOrDefault() ?? FilteredPinItem;
            }

            AddPinsToMap(pins);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert($"{AppResources.Error}", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
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
            if (FilteredPinItem.IsAll)
            {
                pin.IsVisible = true;
                continue;
            }

            if (FilteredPinItem.Name ==(pin.Tag as MapPinTag)?.PinKey)
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

    private async Task InitializeMapPins()
    {
        PinFilterList = new(CustomBoardPinFilterItem.CreateFilterList());

        var arrivalLocationPins = await _arrivalLocationRepository.GetArrivalLocationPinsAsync();
        var arrivalLocationPin = arrivalLocationPins.FirstOrDefault();
        var tag = arrivalLocationPin?.Tag as MapPinTag;
        if (tag is not null)
        {
            PinFilterList.Add(new CustomBoardPinFilterItem(tag.PinKey));
        }

        AddPinsToMap(arrivalLocationPins);


        var boardList = await _customBoardRepository.GetAllCustomBoards();
        foreach (var board in boardList)
        {
            PinFilterList.Add(new CustomBoardPinFilterItem(board.Name));
        }

        var customLocationList = await _customLocationDataRepository.GetAllCustomLocationPins();
        AddPinsToMap(customLocationList);
    }

    private void AddPinsToMap(IEnumerable<Pin> customLocationList)
    {
        foreach (var pin in customLocationList)
        {
            ArrivalMap?.Pins.Add(pin);
        }
    }
}