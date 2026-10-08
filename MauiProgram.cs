using CommunityToolkit.Maui;
using FlagsRally.Helpers;
using FlagsRally.Repository;
using FlagsRally.Services;
using FlagsRally.ViewModels;
using FlagsRally.Views;
using Maui.GoogleMaps.Hosting;
using Maui.RevenueCat.InAppBilling;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Syncfusion.Maui.Core.Hosting;
using System.Runtime.Versioning;

namespace FlagsRally
{
    public static class MauiProgram
    {
        // The plain net10.0 TFM exists only for unit tests and never calls this entry point.
        [SupportedOSPlatform("android21.0")]
        [SupportedOSPlatform("ios15.0")]
        [SupportedOSPlatform("maccatalyst15.0")]
        [SupportedOSPlatform("windows10.0.17763")]
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
                .ConfigureSyncfusionCore()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("JerseyclubGrungeBold-JRXVK.otf", "JerseyclubGrungeBold");
                    fonts.AddFont("craftmincho.otf", "craftmincho");
                    fonts.AddFont("KiwiMaru-Medium.ttf", "KiwiMaru-Medium");
                });

#if ANDROID
            builder.UseGoogleMaps();

            // Workaround for a .NET MAUI 10 Android issue where an ActivityIndicator
            // hidden around the time its handler is created (IsBusy set back to false
            // during startup) keeps its native view visible. Re-sync the native
            // visibility from the cross-platform state once the view has attached.
            Microsoft.Maui.Handlers.ActivityIndicatorHandler.Mapper.AppendToMapping("FixInitialVisibility", (handler, indicator) =>
            {
                handler.PlatformView.Post(() =>
                {
                    var isShown = indicator.IsRunning && (indicator as VisualElement)?.IsVisible != false;
                    handler.PlatformView.Visibility = isShown ? Android.Views.ViewStates.Visible : Android.Views.ViewStates.Gone;
                });
            });
#elif IOS
            builder.UseGoogleMaps(Constants.GOOGLE_MAP_API_KEY);
#endif

            builder.Services.AddRevenueCatBilling();

            builder.Services.AddSingleton<AppShell>();

            builder.Services.AddSingleton<IArrivalLocationDataRepository, ArrivalLocationDataRepository>();
            builder.Services.AddSingleton<SubRegionHelper>();
            builder.Services.AddSingleton<RegionalFlagsService>();
            builder.Services.AddSingleton(Preferences.Default);
            builder.Services.AddSingleton<CustomCountryHelper>();
            builder.Services.AddSingleton<ArrivalLocationService>();
            builder.Services.AddSingleton<CustomBoardService>();
            builder.Services.AddSingleton<MapFocusRequest>();
            builder.Services.AddSingleton<CryptoService>();

            builder.Services.AddSingleton<ICustomLocationDataRepository, CustomLocationDataRepository>();
            builder.Services.AddSingleton<ICustomBoardRepository, CustomBoardRepository>();

            builder.Services.AddSingleton<SettingsPreferences>();
            builder.Services.AddSingleton<CustomGeolocation>();

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<MainPageViewModel>();

            builder.Services.AddSingleton<SettingPage>();
            builder.Services.AddSingleton<SettingPageViewModel>();

            builder.Services.AddSingleton<LocationPage>();
            builder.Services.AddSingleton<LocationPageViewModel>();

            builder.Services.AddSingleton<CollectionsPage>();
            builder.Services.AddSingleton<CollectionsPageViewModel>();

            builder.Services.AddTransient<FlagsBoardPage>();
            builder.Services.AddTransient<FlagsBoardPageViewModel>();

            builder.Services.AddTransient<CustomBoardPage>();
            builder.Services.AddTransient<CustomBoardPageViewModel>();

            builder.Services.AddTransient<ManageCustomBoardsPage>();
            builder.Services.AddTransient<ManageCustomBoardsPageViewModel>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
