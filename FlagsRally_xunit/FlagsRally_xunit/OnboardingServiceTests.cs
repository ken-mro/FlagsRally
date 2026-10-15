using FlagsRally.Models;
using FlagsRally.Models.CustomBoard;
using FlagsRally.Repository;
using FlagsRally.Services;
using Moq;

namespace FlagsRallyTests.Services;

public class OnboardingServiceTests
{
    // An in-memory stand-in for the app's preferences.
    sealed class MemoryPreferences : IPreferences
    {
        readonly Dictionary<string, object?> _values = [];
        public bool ContainsKey(string key, string? sharedName = null) => _values.ContainsKey(key);
        public void Remove(string key, string? sharedName = null) => _values.Remove(key);
        public void Clear(string? sharedName = null) => _values.Clear();
        public void Set<T>(string key, T value, string? sharedName = null) => _values[key] = value;
        public T Get<T>(string key, T defaultValue, string? sharedName = null) =>
            _values.TryGetValue(key, out var value) && value is T typed ? typed : defaultValue;
    }

    static (OnboardingService Service, SettingsPreferences Settings) Create(int arrivals = 0, bool hasBoards = false)
    {
        var settings = new SettingsPreferences(new MemoryPreferences());
        var arrivalRepository = new Mock<IArrivalLocationDataRepository>();
        arrivalRepository.Setup(r => r.GetAllArrivalLocations())
            .ReturnsAsync(Enumerable.Range(0, arrivals).Select(_ => new ArrivalLocation()).ToList());
        var boardRepository = new Mock<ICustomBoardRepository>();
        boardRepository.Setup(r => r.GetCustomBoardExists()).ReturnsAsync(hasBoards);
        return (new OnboardingService(settings, arrivalRepository.Object, boardRepository.Object), settings);
    }

    [Fact]
    public async Task A_new_install_sees_the_pages_until_they_are_closed()
    {
        var (service, settings) = Create();

        Assert.True(await service.ShouldShowOnLaunchAsync());
        Assert.True(await service.ShouldShowOnLaunchAsync());

        settings.SetHasSeenOnboarding();

        Assert.False(await service.ShouldShowOnLaunchAsync());
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(0, true)]
    public async Task Someone_already_using_the_app_is_not_shown_the_pages_after_an_update(int arrivals, bool hasBoards)
    {
        var (service, settings) = Create(arrivals, hasBoards);

        Assert.False(await service.ShouldShowOnLaunchAsync());
        Assert.True(settings.GetHasSeenOnboarding());
    }

    [Fact]
    public void The_long_press_hint_is_shown_until_dismissed()
    {
        var (_, settings) = Create();

        Assert.False(settings.GetHasSeenLongPressHint());
        settings.SetHasSeenLongPressHint();
        Assert.True(settings.GetHasSeenLongPressHint());
    }

    [Theory]
    [InlineData(FlagsRally.ViewModels.OnboardingStep.Passport, "ja-JP", "howto_passport_ja.png")]
    [InlineData(FlagsRally.ViewModels.OnboardingStep.Stamp, "en-US", "howto_stamp_en.png")]
    [InlineData(FlagsRally.ViewModels.OnboardingStep.Boards, "fr-FR", "howto_boards_en.png")]
    [InlineData(FlagsRally.ViewModels.OnboardingStep.HowToAgain, "ja-JP", "howto_settings_ja.png")]
    [InlineData(FlagsRally.ViewModels.OnboardingStep.Location, "ja-JP", "")]
    public void Each_page_shows_the_screenshot_in_the_app_language(FlagsRally.ViewModels.OnboardingStep step, string culture, string expected)
    {
        Assert.Equal(expected, FlagsRally.ViewModels.OnboardingViewModel.ScreenshotFor(step, new System.Globalization.CultureInfo(culture)));
    }

    [Fact]
    public void Every_screenshot_a_page_names_is_in_the_app()
    {
        var images = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Resources", "Images");
        foreach (var step in Enum.GetValues<FlagsRally.ViewModels.OnboardingStep>())
        foreach (var culture in new[] { "en-US", "ja-JP" })
        {
            var name = FlagsRally.ViewModels.OnboardingViewModel.ScreenshotFor(step, new System.Globalization.CultureInfo(culture));
            if (name.Length > 0) Assert.True(File.Exists(Path.Combine(images, name)), name);
        }
    }
}
