using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;
using FlagsRally.Helpers;
using FlagsRally.Resources;

namespace FlagsRally.ViewModels;

public enum OnboardingStep { Passport, Stamp, Boards, Location }

/// <summary>
/// One dot under the how-to pages; the current page's dot is filled.
/// </summary>
public partial class OnboardingDot : ObservableObject
{
    [ObservableProperty]
    bool _isCurrent;
}

/// <summary>
/// The how-to pages: the passport, stamping on the map, boards, and the location permission.
/// </summary>
public partial class OnboardingViewModel : ObservableObject
{
    static readonly OnboardingStep[] Steps = Enum.GetValues<OnboardingStep>();

    readonly Func<Task> _close;

    public OnboardingViewModel(Func<Task> close)
    {
        _close = close;
        Dots = Steps.Select(_ => new OnboardingDot()).ToList();
        UpdateDots();
    }

    public IReadOnlyList<OnboardingDot> Dots { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Step), nameof(Title), nameof(Body), nameof(Note), nameof(HasNote), nameof(IsFirst), nameof(IsNotFirst), nameof(IsLast), nameof(IsNotLast))]
    [NotifyPropertyChangedFor(nameof(Screenshot), nameof(HasScreenshot), nameof(HasNoScreenshot))]
    [NotifyPropertyChangedFor(nameof(ShowsAllowLocation), nameof(ShowsDone))]
    int _index;

    partial void OnIndexChanged(int value) => UpdateDots();

    /// <summary>
    /// Whether the app may already use the location; the last page then just closes.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsAllowLocation), nameof(ShowsDone))]
    bool _isLocationAllowed;

    public OnboardingStep Step => Steps[Index];
    public bool IsFirst => Index == 0;
    public bool IsNotFirst => !IsFirst;
    public bool IsLast => Index == Steps.Length - 1;
    public bool IsNotLast => !IsLast;

    /// <summary>
    /// The screenshot for the page in the app's language (Resources/Images/howto_*), or empty for the
    /// location page, which has an icon.
    /// </summary>
    public string Screenshot => ScreenshotFor(Step, CultureInfo.CurrentUICulture);
    public bool HasScreenshot => Screenshot.Length > 0;
    public bool HasNoScreenshot => !HasScreenshot;

    public static string ScreenshotFor(OnboardingStep step, CultureInfo culture)
    {
        var page = step switch
        {
            OnboardingStep.Passport => "passport",
            OnboardingStep.Stamp => "stamp",
            OnboardingStep.Boards => "boards",
            _ => null,
        };
        var language = culture.TwoLetterISOLanguageName == "ja" ? "ja" : "en";
        return page is null ? string.Empty : $"howto_{page}_{language}.png";
    }

    public bool ShowsAllowLocation => IsLast && !IsLocationAllowed;
    public bool ShowsDone => IsLast && IsLocationAllowed;

    public string Title => Step switch
    {
        OnboardingStep.Passport => AppResources.OnboardingPassportTitle,
        OnboardingStep.Stamp => AppResources.OnboardingStampTitle,
        OnboardingStep.Boards => AppResources.OnboardingBoardsTitle,
        _ => AppResources.OnboardingLocationTitle,
    };

    public string Body => Step switch
    {
        OnboardingStep.Passport => AppResources.OnboardingPassportBody,
        OnboardingStep.Stamp => string.Format(AppResources.OnboardingStampBody, CheckInRange.LimitText),
        OnboardingStep.Boards => string.Format(AppResources.OnboardingBoardsBody, CheckInRange.LimitText),
        _ => AppResources.OnboardingLocationBody,
    };

    /// <summary>
    /// A side note under the text, shown smaller (only the passport page has one: changing the cover).
    /// </summary>
    public string Note => Step == OnboardingStep.Passport ? AppResources.OnboardingPassportNote : string.Empty;
    public bool HasNote => Note.Length > 0;

    public async Task RefreshLocationPermissionAsync()
    {
        IsLocationAllowed = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>() == PermissionStatus.Granted;
    }

    [RelayCommand]
    void Next()
    {
        if (!IsLast) Index++;
    }

    /// <summary>
    /// Goes back a page; returns false on the first page.
    /// </summary>
    public bool GoBack()
    {
        if (IsFirst) return false;
        Index--;
        return true;
    }

    [RelayCommand]
    void Back() => GoBack();

    [RelayCommand]
    Task FinishAsync() => _close();

    [RelayCommand]
    async Task AllowLocationAsync()
    {
        await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        await _close();
    }

    void UpdateDots()
    {
        for (int i = 0; i < Dots.Count; i++)
        {
            Dots[i].IsCurrent = i == Index;
        }
    }
}
