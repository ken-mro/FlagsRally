namespace FlagsRally.ViewModels;

/// <summary>A page of a popup with several, shown as a dot (PageDots); the current one is navy.</summary>
public interface IPageDot
{
    bool IsCurrent { get; }
}
