using FlagsRally.Models.CustomBoard;
using FlagsRally.Repository;
using FlagsRally.Resources;
using FlagsRally.ViewModels;
using FlagsRally.Views;
using CommunityToolkit.Maui.Extensions;

namespace FlagsRally.Services;

/// <summary>
/// Opens a visited place's stamp, from the map's pin or a board's tile. A stamp checked in without its
/// image (no signal) is downloaded now and pressed down with confetti the first time it can be shown,
/// wherever it is first opened, and from then on it is shown without the celebration.
/// </summary>
public class StampViewer(ICustomLocationDataRepository repository, StampImageLoader loader)
{
    /// <summary>
    /// Downloads a pending stamp's image and, once it is here, records the stamp as seen so it is
    /// celebrated only once. Null when the image still cannot be had; the stamp then stays pending.
    /// </summary>
    public async Task<byte[]?> LoadPendingStampAsync(CustomLocation place)
    {
        var image = await loader.LoadAsync(place.ImageUrl);
        if (image is not null) await repository.MarkStampSeen(place.CompositeKey);
        return image;
    }

    /// <summary>
    /// Shows the place's stamp in its popup. <paramref name="setLoading"/> is told while a pending stamp is
    /// downloaded, so the page can show its spinner.
    /// </summary>
    public async Task ShowAsync(string key, Action<bool> setLoading)
    {
        var place = await repository.GetCustomLocationByCompositeKey(key);
        if (place is null) return;
        if (!place.IsStampPending || string.IsNullOrEmpty(place.ImageUrl))
        {
            await ShowPopupAsync(place, celebrates: false);
            return;
        }

        setLoading(true);
        byte[]? image;
        try
        {
            image = await LoadPendingStampAsync(place);
        }
        finally
        {
            setLoading(false);
        }
        if (image is null)
        {
            await AppDialogs.AlertAsync(AppResources.StampImageNotLoadedTitle, AppResources.StampImageNotLoaded);
            return;
        }
        await ShowPopupAsync(place, celebrates: true, image);
    }

    static async Task ShowPopupAsync(CustomLocation place, bool celebrates, byte[]? image = null)
    {
        var popup = new CustomLocationImagePopupView(new CustomLocationImagePopupViewModel(place, celebrates, image));
        await Shell.Current.CurrentPage.ShowPopupAsync(popup);
    }
}
