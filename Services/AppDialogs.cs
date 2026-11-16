using CommunityToolkit.Maui.Extensions;
using FlagsRally.Resources;
using FlagsRally.ViewModels;
using FlagsRally.Views;

namespace FlagsRally.Services;

/// <summary>
/// The app's notices, questions and short lists, shown in its own popups (paper card, stamp
/// heading) rather than the system's alert and action sheet, which look different on each platform.
/// </summary>
public static class AppDialogs
{
    /// <summary>A notice with OK.</summary>
    public static Task AlertAsync(string title, string message) =>
        ShowAsync(new MessagePopupViewModel(title, message, AppResources.OK));

    /// <summary>A question; true for <paramref name="accept"/>, false for <paramref name="cancel"/> or a tap outside.</summary>
    public static async Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
    {
        var viewModel = new MessagePopupViewModel(title, message, accept, cancel);
        await ShowAsync(viewModel);
        return viewModel.Accepted;
    }

    /// <summary>
    /// One of <paramref name="labels"/> to pick, the one in use ticked. The index picked, or null
    /// when closed without picking.
    /// </summary>
    public static async Task<int?> PickOneAsync(string title, IReadOnlyList<string> labels, int? current = null)
    {
        var viewModel = ChoicePopupViewModel.PickOne(title, labels, current);
        await Shell.Current.CurrentPage.ShowPopupAsync(new ChoicePopupView(viewModel));
        return viewModel.PickedIndex;
    }

    /// <summary>
    /// Options with a tick box each. <paramref name="switched"/> is called with the option's index
    /// and new state as soon as it is tapped; the task ends when the popup closes.
    /// </summary>
    public static Task OptionsAsync(string title, IReadOnlyList<(string Label, bool IsOn)> options, Action<int, bool> switched) =>
        Shell.Current.CurrentPage.ShowPopupAsync(new ChoicePopupView(ChoicePopupViewModel.Options(title, options, switched)));

    static Task ShowAsync(MessagePopupViewModel viewModel) =>
        Shell.Current.CurrentPage.ShowPopupAsync(new MessagePopupView(viewModel));
}
