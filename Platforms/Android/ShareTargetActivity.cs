using Android.App;
using Android.Content;
using Android.OS;

namespace FlagsRally
{
    // Receives a place shared from another app (Google Maps' Share, for one) and hands it to the app's
    // screen. A share starts its target inside the sharing app's task, so this small activity does that
    // and closes, and the one MainActivity (in its own task) shows the place on the map. It shows the
    // splash, as a launch does: starting the app can take a while, and a blank screen meanwhile is
    // taken for a hang. It has no task affinity: in the app's own task it would be swallowed by the
    // screen already there and never run.
    [Activity(Exported = true, NoHistory = true, ExcludeFromRecents = true, TaskAffinity = "", Theme = "@style/Maui.SplashTheme")]
    [IntentFilter([Intent.ActionSend], Categories = [Intent.CategoryDefault], DataMimeType = "text/plain")]
    public class ShareTargetActivity : Activity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var text = Intent?.GetStringExtra(Intent.ExtraText);
            if (!string.IsNullOrWhiteSpace(text))
            {
                var main = new Intent(this, typeof(MainActivity));
                main.PutExtra(MainActivity.SharedTextExtra, text);
                main.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop | ActivityFlags.SingleTop);
                StartActivity(main);
            }
            Finish();
        }
    }
}
