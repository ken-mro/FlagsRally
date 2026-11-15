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
    static SettingsPreferences Preferences(bool savedSubscribed)
    {
        var store = new Dictionary<string, string> { ["IsSubscribed"] = savedSubscribed.ToString() };
        var preferences = new Mock<IPreferences>();
        preferences.Setup(p => p.Get(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string key, string fallback, string? _) => store.TryGetValue(key, out var value) ? value : fallback);
        preferences.Setup(p => p.Set(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback((string key, string value, string? _) => store[key] = value);
        return new SettingsPreferences(preferences.Object);
    }

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
}
