using CommunityToolkit.Maui.Maps;
using FlagsRally.Repository;
using FlagsRally.ViewModels;
using FlagsRally.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace FlagsRally
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
#if ANDROID || IOS
                .UseMauiMaps()
#elif WINDOWS
                .UseMauiCommunityToolkitMaps("PASTE-YOUR-API-KEY-HERE")
#endif
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton(Preferences.Default);

            builder.Services.AddSingleton<SettingsPreferences>();

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<MainPageViewModel>();

            builder.Services.AddSingleton<SettingPage>();
            builder.Services.AddSingleton<SettingPageViewModel>();

            builder.Services.AddSingleton<LocationPage>();
            builder.Services.AddSingleton<LocationPageViewModel>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
