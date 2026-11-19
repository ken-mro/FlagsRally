using FlagsRally.Models.CustomBoard;
using FlagsRally.Views;

namespace FlagsRally.Controls;

/// <summary>
/// A page that throws confetti over itself when a stamp lands on one of its tiles.
/// </summary>
public interface ICelebrationHost
{
    void Celebrate();
}

/// <summary>
/// A tile's stamp pressed down where it is: when its place's stamp is pressed (<see cref="CustomLocation.PressStamp"/>),
/// it comes in with the popup's press and the page throws confetti as it lands.
/// </summary>
public class PressedStampImage : Image
{
    bool _waiting;

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(Source))
        {
            if (Source is not null && BindingContext is CustomLocation place && place.TakePressAnimation())
                Prepare();
        }
        else if (propertyName == nameof(IsLoading) && _waiting && !IsLoading)
        {
            _ = PressAsync();
        }
    }

    // Hidden until decoded, so the press does not land on an empty frame
    void Prepare()
    {
        _waiting = true;
        StampPress.Prepare(this);
        // Bytes already in memory decode quickly and may never report loading; the stamp must not stay hidden
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(400), () => _ = PressAsync());
    }

    async Task PressAsync()
    {
        if (!_waiting) return;
        _waiting = false;
        await StampPress.PressAsync(this, () => FindHost()?.Celebrate());
    }

    ICelebrationHost? FindHost()
    {
        for (Element? e = Parent; e is not null; e = e.Parent)
            if (e is ICelebrationHost host) return host;
        return null;
    }
}
