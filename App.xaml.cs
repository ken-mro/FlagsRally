using FlagsRally.Repository;
using Maui.RevenueCat.InAppBilling.Services;

namespace FlagsRally;

public partial class App : Application
{
    private readonly IRevenueCatBilling _revenueCat;
    private readonly ICustomBoardRepository _customBoardRepository;
    public App(IRevenueCatBilling revenueCatBilling, ICustomBoardRepository customBoardRepository)
    {
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Constants.SYNCFUSIOHN_LICENSE_KEY);
        InitializeComponent();
        _revenueCat = revenueCatBilling;
        _customBoardRepository = customBoardRepository;
    }
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell(_customBoardRepository));
    }

    protected override void OnStart()
    {
        var revenueCatApiKey = string.Empty;

#if __ANDROID__
        revenueCatApiKey = Constants.REVENUECAT_API_KEY_ANDROID;
#elif __IOS__
        revenueCatApiKey = Constants.REVENUECAT_API_KEY_IOS;
#endif

        _revenueCat.Initialize(revenueCatApiKey);

        base.OnStart();
    }
}
