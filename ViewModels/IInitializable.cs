namespace FlagsRally.ViewModels;

/// <summary>A page's view model that loads (again) each time its page is shown.</summary>
public interface IInitializable
{
    Task Init();
}
