using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FlagsRally.ViewModels;

/// <summary>
/// A popup that shows an arrival's stamps one page at a time (StampSlidesView): the country's,
/// then the region's when there is one, swiped from one to the other.
/// </summary>
public abstract partial class StampSlidesViewModel : BaseViewModel
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

    protected StampSlidesViewModel(IReadOnlyList<StampSlide> slides)
    {
        if (slides.Count == 0) throw new ArgumentException("A stamp popup needs a page", nameof(slides));
        Slides = slides;
        Slides[0].IsCurrent = true;
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
}
