using FlagsRally.Repository;
using Maui.RevenueCat.InAppBilling.Services;

namespace FlagsRally.Services;

/// <summary>
/// Whether the user subscribes, asked of RevenueCat and saved for the next start.
/// </summary>
public static class SubscriptionStatus
{
    /// <summary>
    /// RevenueCat's answer, saved and returned. Without an answer (offline, or RevenueCat reports an
    /// error) the last saved status stands: a subscriber who opens the app offline stays a subscriber.
    /// </summary>
    public static async Task<bool> RefreshAsync(IRevenueCatBilling billing, SettingsPreferences preferences)
    {
        try
        {
            var customerInfo = await billing.GetCustomerInfo();
            if (customerInfo.IsSuccess && customerInfo.Value is not null)
            {
                var isSubscribed = customerInfo.Value.ActiveSubscriptions?.Count > 0;
                preferences.SetIsSubscribed(isSubscribed);
                return isSubscribed;
            }
        }
        catch (Exception)
        {
            // No answer: keep the saved status
        }
        return preferences.GetIsSubscribed();
    }
}
