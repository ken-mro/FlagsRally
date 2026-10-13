using FlagsRally.Repository;

namespace FlagsRally.Services;

/// <summary>
/// Decides when the how-to pages open by themselves: once, on a new install.
/// </summary>
public class OnboardingService
{
    readonly SettingsPreferences _settingsPreferences;
    readonly IArrivalLocationDataRepository _arrivalLocationRepository;
    readonly ICustomBoardRepository _customBoardRepository;

    public OnboardingService(SettingsPreferences settingsPreferences, IArrivalLocationDataRepository arrivalLocationRepository, ICustomBoardRepository customBoardRepository)
    {
        _settingsPreferences = settingsPreferences;
        _arrivalLocationRepository = arrivalLocationRepository;
        _customBoardRepository = customBoardRepository;
    }

    /// <summary>
    /// True the first time the app starts after being installed. Someone who already has stamps or
    /// boards was using the app before these pages existed, so they are not shown the pages on update.
    /// </summary>
    public async Task<bool> ShouldShowOnLaunchAsync()
    {
        if (_settingsPreferences.GetHasSeenOnboarding()) return false;

        if ((await _arrivalLocationRepository.GetAllArrivalLocations()).Count > 0
            || await _customBoardRepository.GetCustomBoardExists())
        {
            _settingsPreferences.SetHasSeenOnboarding();
            return false;
        }
        return true;
    }
}
