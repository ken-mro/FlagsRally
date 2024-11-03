using Android.App;
using Android.Runtime;
using FlagsRally.Repository;

namespace FlagsRally
{
    [Application]
    [MetaData("com.google.android.maps.v2.API_KEY",
            Value = Constants.GOOGLE_MAP_API_KEY)]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
