using CommunityToolkit.Mvvm.Input;

namespace FlagsRally.ViewModels;

public enum VisitFilter { All, Visited, Unvisited }

/// <summary>
/// A board whose places can be narrowed to visited or unvisited ones with <see cref="Controls.VisitFilterBar"/>.
/// </summary>
public interface IVisitFilterable
{
    bool ShowsAll { get; }
    bool ShowsVisited { get; }
    bool ShowsUnvisited { get; }
    IRelayCommand<string> SetVisitFilterCommand { get; }
}

public static class VisitFilterExtensions
{
    public static bool Matches(this VisitFilter filter, bool hasBeenVisited) => filter switch
    {
        VisitFilter.Visited => hasBeenVisited,
        VisitFilter.Unvisited => !hasBeenVisited,
        _ => true,
    };
}
