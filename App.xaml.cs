using FlagsRally.Repository;

namespace FlagsRally;

public partial class App : Application
{
    public App(IArrivalLocationDataRepository arrivalLocationRepository)
    {
        InitializeComponent();
        _ = arrivalLocationRepository.UpdateDatabase();
        MainPage = new AppShell();
    }
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = base.CreateWindow(activationState);

        const int newWidth = 600;
        const int newHeight = 800;

        window.MinimumHeight = window.Height = newHeight;
        window.MinimumWidth = window.Width = newWidth;

        return window;
    }
}
