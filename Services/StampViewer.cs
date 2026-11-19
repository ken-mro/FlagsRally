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
    /// <see cref="LoadPendingStampAsync"/> for a tap: <paramref name="setLoading"/> is told while it downloads,
    /// so the page can show its spinner, and a stamp that still cannot be had is explained in a notice.
    /// </summary>
    public async Task<byte[]?> LoadPendingStampWithNoticeAsync(CustomLocation place, Action<bool> setLoading)
    {
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
            await AppDialogs.AlertAsync(AppResources.StampImageNotLoadedTitle, AppResources.StampImageNotLoaded);
        return image;
    }

    /// <summary>
    /// Shows the place's stamp in its popup, pressed down with confetti if it was still pending.
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

        if (await LoadPendingStampWithNoticeAsync(place, setLoading) is { } image)
            await ShowPopupAsync(place, celebrates: true, image);
    }

    /// <summary>
    /// The stamp popup: with confetti when <paramref name="celebrates"/>, from <paramref name="image"/> when it
    /// was just downloaded, and with "Checked in" and where to see it later when <paramref name="stampUnseen"/>.
    /// </summary>
    public static async Task ShowPopupAsync(CustomLocation place, bool celebrates, byte[]? image = null, bool stampUnseen = false)
    {
        var popup = new CustomLocationImagePopupView(new CustomLocationImagePopupViewModel(place, celebrates, image, stampUnseen));
        await Shell.Current.CurrentPage.ShowPopupAsync(popup);
    }
}
