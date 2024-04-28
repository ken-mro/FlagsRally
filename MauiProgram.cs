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
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            var baseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);


            var configuration = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(baseDirectory, @"Resources\countries.json")) // Injecting the countries.json file
                .Build();

            builder.Configuration.AddConfiguration(configuration);

            builder.Services.AddSingleton<CountryRepository>();

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
