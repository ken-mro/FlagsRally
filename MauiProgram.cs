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

namespace FlagsRally
{
    public static class MauiProgram
    {
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
#elif IOS
            builder.UseGoogleMaps(Constants.GOOGLE_MAP_API_KEY);
#endif

            builder.Services.AddRevenueCatBilling();
            
            builder.Services.AddSingleton<IArrivalLocationDataRepository, ArrivalLocationDataRepository>();
            builder.Services.AddSingleton<SubRegionHelper>();
            builder.Services.AddSingleton(Preferences.Default);
            builder.Services.AddSingleton<CustomCountryHelper>();
            builder.Services.AddSingleton<ArrivalLocationService>();
            builder.Services.AddSingleton<CustomBoardService>();
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

            builder.Services.AddSingleton<FlagsBoardPage>();
            builder.Services.AddSingleton<FlagsBoardPageViewModel>();

            builder.Services.AddTransient<CustomBoardPage>();
            builder.Services.AddTransient<CustomBoardPageViewModel>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
