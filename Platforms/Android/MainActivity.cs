using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using CommunityToolkit.Mvvm.Messaging;
using FlagsRally.Messages;
using FlagsRally.Services;

namespace FlagsRally
{
    // SingleTop: starting the app while it is already in front (from a notification, a link or
    // another app) resumes this screen instead of stacking a second one with a fresh, reset map.
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        // The location permission is not asked for here, at launch: the how-to pages explain it and ask
        // on their last page, and otherwise it is asked the first time the location is needed.

        // Set by ShareTargetActivity: what another app shared, for the map to show
        public const string SharedTextExtra = "FlagsRally.SharedText";

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Not when the screen is rebuilt (rotation, process restored): that place was shown already
            if (savedInstanceState is null) ShowSharedPlace(Intent);
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            ShowSharedPlace(intent);
        }

        static void ShowSharedPlace(Intent? intent)
        {
            var text = intent?.GetStringExtra(SharedTextExtra);
            if (string.IsNullOrWhiteSpace(text)) return;
            intent!.RemoveExtra(SharedTextExtra);

            IPlatformApplication.Current?.Services.GetService<MapFocusRequest>()?.RequestSharedPlace(text);
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (Shell.Current is not null) await Shell.Current.GoToAsync("//Map");
                WeakReferenceMessenger.Default.Send(new SharedPlaceReceivedMessage());
            });
        }
    }
}
