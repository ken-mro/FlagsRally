using Android.App;
using Android.Content.PM;

namespace FlagsRally
{
    // SingleTop: starting the app while it is already in front (from a notification, a link or
    // another app) resumes this screen instead of stacking a second one with a fresh, reset map.
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        // The location permission is not asked for here, at launch: the how-to pages explain it and ask
        // on their last page, and otherwise it is asked the first time the location is needed.
    }
}
