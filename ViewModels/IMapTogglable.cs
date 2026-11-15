using CommunityToolkit.Mvvm.Input;

namespace FlagsRally.ViewModels;

/// <summary>A page that switches between its stamps and a map (MapToggleButton).</summary>
public interface IMapTogglable
{
    bool IsListVisible { get; }
    bool IsMapVisible { get; }
    IAsyncRelayCommand ChangeMapVisibilityCommand { get; }
}
