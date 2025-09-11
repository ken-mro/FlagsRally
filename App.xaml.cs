using FlagsRally.Repository;
using Maui.RevenueCat.InAppBilling.Services;

namespace FlagsRally;

public partial class App : Application
{
    private readonly IRevenueCatBilling _revenueCat;
    private readonly AppShell _appShell;
    public App(IRevenueCatBilling revenueCatBilling, AppShell appShell)
    {
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Constants.SYNCFUSIOHN_LICENSE_KEY);
        InitializeComponent();
        _revenueCat = revenueCatBilling;
        _appShell = appShell;
    }
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(_appShell);
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
