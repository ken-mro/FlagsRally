using Android.App;
using Android.Runtime;
using CommunityToolkit.Maui.ApplicationModel;
using CommunityToolkit.Maui.Sample;

namespace FlagsRally
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
            var samsungProvider = new SamsungBadgeProvider();

            BadgeFactory.SetBadgeProvider("com.sec.android.app.launcher", samsungProvider);
            BadgeFactory.SetBadgeProvider("com.sec.android.app.twlauncher", samsungProvider);
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
