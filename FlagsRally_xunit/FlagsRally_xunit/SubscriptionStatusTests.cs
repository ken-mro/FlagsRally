using FlagsRally.Repository;
using FlagsRally.Services;
using Maui.RevenueCat.InAppBilling.Enums;
using Maui.RevenueCat.InAppBilling.Models;
using Maui.RevenueCat.InAppBilling.Services;
using Moq;

namespace FlagsRallyTests.Services;

public class SubscriptionStatusTests
{
    // Preferences kept in a dictionary, so what is saved can be read back
    static SettingsPreferences Preferences(bool savedSubscribed, TimeProvider? clock = null)
    {
        var store = new Dictionary<string, string> { ["IsSubscribed"] = savedSubscribed.ToString() };
        var preferences = new Mock<IPreferences>();
        preferences.Setup(p => p.Get(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string key, string fallback, string? _) => store.TryGetValue(key, out var value) ? value : fallback);
        preferences.Setup(p => p.Set(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback((string key, string value, string? _) => store[key] = value);
        return new SettingsPreferences(preferences.Object, clock);
    }

    // A clock the test moves, forwards or back
    sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    static readonly CustomerInfoResultDto NoAnswer = new() { Error = PurchaseErrorStatus.NetworkError };

    static IRevenueCatBilling Billing(CustomerInfoResultDto result)
    {
        var billing = new Mock<IRevenueCatBilling>();
        billing.Setup(b => b.GetCustomerInfo(It.IsAny<CancellationToken>())).ReturnsAsync(result);
        return billing.Object;
    }

    static CustomerInfoResultDto Answer(params string[] activeSubscriptions) =>
        new()
        {
            Value = new CustomerInfoDto
            {
                ActiveSubscriptions = [.. activeSubscriptions],
                AllPurchasedIdentifiers = [],
                NonConsumablePurchases = [],
                FirstSeen = null,
                LatestExpirationDate = null,
                ManagementUrl = null!,
                Entitlements = [],
            },
        };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task An_answer_from_RevenueCat_is_saved(bool savedBefore)
    {
        var preferences = Preferences(savedBefore);

        Assert.True(await SubscriptionStatus.RefreshAsync(Billing(Answer("yearly")), preferences));
        Assert.True(preferences.GetIsSubscribed());

        Assert.False(await SubscriptionStatus.RefreshAsync(Billing(Answer()), preferences));
        Assert.False(preferences.GetIsSubscribed());
    }

    // Offline, RevenueCat answers with an error: a subscriber must not become "not subscribed"
    [Fact]
    public async Task Without_an_answer_the_saved_status_stands()
    {
        var preferences = Preferences(savedSubscribed: true);
        var failed = new CustomerInfoResultDto { Error = PurchaseErrorStatus.NetworkError };

        Assert.True(await SubscriptionStatus.RefreshAsync(Billing(failed), preferences));
        Assert.True(preferences.GetIsSubscribed());
    }

    [Fact]
    public async Task When_asking_RevenueCat_throws_the_saved_status_stands()
    {
        var preferences = Preferences(savedSubscribed: true);
        var billing = new Mock<IRevenueCatBilling>();
        billing.Setup(b => b.GetCustomerInfo(It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException());

        Assert.True(await SubscriptionStatus.RefreshAsync(billing.Object, preferences));
        Assert.True(preferences.GetIsSubscribed());
    }

    // A trip offline: the saved status counts until the grace ends
    [Theory]
    [InlineData(0, true)]
    [InlineData(13, true)]
    [InlineData(14, true)]
    [InlineData(15, false)]
    [InlineData(400, false)]
    public async Task Without_an_answer_a_subscriber_stays_one_only_for_the_grace(int daysWithoutAnswer, bool subscribed)
    {
        var clock = new Clock();
        var preferences = Preferences(savedSubscribed: false, clock);
        Assert.True(await SubscriptionStatus.RefreshAsync(Billing(Answer("yearly")), preferences));

        clock.Now += TimeSpan.FromDays(daysWithoutAnswer);

        Assert.Equal(subscribed, await SubscriptionStatus.RefreshAsync(Billing(NoAnswer), preferences));
        Assert.Equal(subscribed, preferences.GetIsSubscribed());
        Assert.Equal(subscribed, preferences.CanChooseStampStyle());
    }

    [Fact]
    public async Task An_answer_after_the_grace_makes_the_user_a_subscriber_again()
    {
        var clock = new Clock();
        var preferences = Preferences(savedSubscribed: false, clock);
        await SubscriptionStatus.RefreshAsync(Billing(Answer("yearly")), preferences);
        clock.Now += TimeSpan.FromDays(30);
        Assert.False(await SubscriptionStatus.RefreshAsync(Billing(NoAnswer), preferences));

        Assert.True(await SubscriptionStatus.RefreshAsync(Billing(Answer("yearly")), preferences));

        clock.Now += TimeSpan.FromDays(10);
        Assert.True(preferences.GetIsSubscribed());
    }

    // Setting the device's clock back must not bring a status back after the grace has ended
    [Fact]
    public async Task Moving_the_clock_back_does_not_lengthen_the_grace()
    {
        var clock = new Clock();
        var preferences = Preferences(savedSubscribed: false, clock);
        await SubscriptionStatus.RefreshAsync(Billing(Answer("yearly")), preferences);
        var answered = clock.Now;

        clock.Now = answered + TimeSpan.FromDays(20);
        Assert.False(preferences.GetIsSubscribed());

        clock.Now = answered + TimeSpan.FromDays(1);
        Assert.False(preferences.GetIsSubscribed());

        clock.Now = answered - TimeSpan.FromDays(300);
        Assert.False(preferences.GetIsSubscribed());
    }

    // A clock set back within the grace (a traveller correcting it) does not end the subscription
    [Fact]
    public async Task Moving_the_clock_back_within_the_grace_keeps_the_subscriber()
    {
        var clock = new Clock();
        var preferences = Preferences(savedSubscribed: false, clock);
        await SubscriptionStatus.RefreshAsync(Billing(Answer("yearly")), preferences);
        var answered = clock.Now;

        clock.Now = answered + TimeSpan.FromDays(5);
        Assert.True(preferences.GetIsSubscribed());

        clock.Now = answered - TimeSpan.FromDays(2);
        Assert.True(preferences.GetIsSubscribed());

        // Days count from the latest time seen (day 5), not from the clock that was set back
        clock.Now = answered + TimeSpan.FromDays(14);
        Assert.True(preferences.GetIsSubscribed());
        clock.Now = answered + TimeSpan.FromDays(15);
        Assert.False(preferences.GetIsSubscribed());
    }

    // A status saved by an earlier version has no time: the grace starts at the first look
    [Fact]
    public void A_status_saved_without_a_time_starts_its_grace_when_first_read()
    {
        var clock = new Clock();
        var preferences = Preferences(savedSubscribed: true, clock);

        Assert.True(preferences.GetIsSubscribed());

        clock.Now += TimeSpan.FromDays(14);
        Assert.True(preferences.GetIsSubscribed());
        clock.Now += TimeSpan.FromDays(1);
        Assert.False(preferences.GetIsSubscribed());
    }

    [Fact]
    public async Task Not_subscribed_stays_not_subscribed_without_an_answer()
    {
        var clock = new Clock();
        var preferences = Preferences(savedSubscribed: false, clock);
        await SubscriptionStatus.RefreshAsync(Billing(Answer()), preferences);

        clock.Now += TimeSpan.FromDays(1);

        Assert.False(await SubscriptionStatus.RefreshAsync(Billing(NoAnswer), preferences));
    }
}
