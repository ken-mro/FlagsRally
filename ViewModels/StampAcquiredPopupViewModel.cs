using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlagsRally.Models;
using FlagsRally.Resources;

namespace FlagsRally.ViewModels;

/// <summary>
/// One page of the popup after a stamp: the country's stamp or the region's, in the passport's design.
/// </summary>
public partial class StampSlide(ArrivalLocation arrival, string heading, string name, string flagEmoji, string flagImage, bool celebrates) : ObservableObject
{
    /// <summary>The arrival with the pattern the passport draws it in, for its ink and padding.</summary>
    public ArrivalLocation Arrival { get; } = arrival;
    public string Heading { get; } = heading;
    /// <summary>Confetti when pressed: only for a country or region stamped for the first time.</summary>
    public bool Celebrates { get; } = celebrates;
    public string Name { get; } = name;
    public string FlagEmoji { get; } = flagEmoji;
    public string FlagImage { get; } = flagImage;

    public bool HasFlagEmoji => !string.IsNullOrEmpty(FlagEmoji);
    public bool HasFlagImage => !HasFlagEmoji && !string.IsNullOrEmpty(FlagImage);

    public bool IsSquare => Arrival.StampPattern == StampPattern.Square;
    public bool IsJourney => Arrival.StampPattern == StampPattern.Journey;
    public bool IsShaped => !IsSquare && !IsJourney;

    // The dot of the page being shown
    [ObservableProperty]
    bool _isCurrent;
}

/// <summary>
/// The popup after every stamp: the country's stamp and, in a country with regional flags, the
/// region's on the next page, each pressed down in turn, with confetti only for a first one. OK comes
/// on once every page has been pressed.
/// </summary>
public partial class StampAcquiredPopupViewModel : BaseViewModel
{
    public Popup? Popup { get; set; }

    public IReadOnlyList<StampSlide> Slides { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Current), nameof(IsLast), nameof(HasMore))]
    int _index;

    public StampSlide Current => Slides[Index];
    public bool IsLast => Index == Slides.Count - 1;
    public bool HasMore => !IsLast;
    public bool HasSeveral => Slides.Count > 1;

    // Set by the view once the last page's stamp has been pressed
    [ObservableProperty]
    bool _canClose;

    public StampAcquiredPopupViewModel(IReadOnlyList<StampSlide> slides)
    {
        if (slides.Count == 0) throw new ArgumentException("A stamp popup needs a page", nameof(slides));
        Slides = slides;
        Slides[0].IsCurrent = true;
    }

    /// <summary>
    /// The pages for an arrival, in the design the passport draws it in (upright): the country's stamp,
    /// then, in a country with regional flags, the region's. Each is pressed down; only one stamped
    /// there for the first time gets confetti and "New", the others say "Stamped". With
    /// <paramref name="everyStamp"/> off, only the new ones get a page, and none when nothing is new.
    /// </summary>
    public static List<StampSlide> SlidesFor(ArrivalLocation arrival, StampStyle style, bool newCountry, bool hasRegions, bool newRegion, bool everyStamp = true)
    {
        var stamp = StampPatterns.Apply(arrival, style, tilted: false);
        var slides = new List<StampSlide>();
        if (newCountry || everyStamp)
        {
            slides.Add(new(stamp, newCountry ? AppResources.NewCountry : AppResources.Stamped, stamp.CountryName, stamp.CountryFlagSource, string.Empty, celebrates: newCountry));
        }
        if (hasRegions && (newRegion || everyStamp) && !string.IsNullOrEmpty(stamp.AdminAreaName))
        {
            slides.Add(new(stamp, newRegion ? AppResources.NewRegion : AppResources.Stamped, stamp.AdminAreaName, string.Empty, stamp.AdminAreaFlagSource, celebrates: newRegion));
        }
        return slides;
    }

    /// <summary>Moves by a page; false at either end.</summary>
    public bool Move(int step)
    {
        var next = Index + step;
        if (next < 0 || next >= Slides.Count) return false;
        Slides[Index].IsCurrent = false;
        Index = next;
        Slides[Index].IsCurrent = true;
        return true;
    }

    [RelayCommand]
    async Task ClosePopup()
    {
        if (CanClose && Popup is not null)
        {
            await Popup.CloseAsync();
        }
    }
}
