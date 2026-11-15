using CommunityToolkit.Mvvm.ComponentModel;
using FlagsRally.Resources;

namespace FlagsRally.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    string _title = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    bool _isBusy = false;

    public bool IsNotBusy => !IsBusy;

    /// <summary>An alert with OK.</summary>
    protected static Task ShowAlertAsync(string title, string message) =>
        Shell.Current.DisplayAlertAsync(title, message, AppResources.OK);

    /// <summary>An error alert with OK.</summary>
    protected static Task ShowErrorAsync(string message) => ShowAlertAsync(AppResources.Error, message);

    /// <summary>
    /// Runs <paramref name="work"/> with the page busy (its spinner shown), and shows what went wrong
    /// in an error alert, with "Please try again" when <paramref name="askToRetry"/>.
    /// </summary>
    protected async Task RunBusyAsync(Func<Task> work, bool askToRetry = false)
    {
        try
        {
            IsBusy = true;
            await work();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(askToRetry ? $"{ex.Message}\n{AppResources.PleaseTryAgain}" : ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Asks Yes or No; true for Yes.</summary>
    protected static Task<bool> ConfirmAsync(string title, string message) =>
        Shell.Current.DisplayAlertAsync(title, message, AppResources.Yes, AppResources.No);
}