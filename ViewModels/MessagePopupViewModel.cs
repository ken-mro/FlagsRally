using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;

namespace FlagsRally.ViewModels;

/// <summary>
/// A message in the app's own popup, in place of the system alert: a heading, the text, a filled
/// button and, for a question, a quiet second one under it. The caller reads
/// <see cref="Accepted"/> once the popup has closed: true only for the filled button (the second
/// button and a tap outside leave it false).
/// </summary>
public partial class MessagePopupViewModel(string title, string message, string accept, string? cancel = null) : BaseViewModel
{
    public Popup? Popup { get; set; }

    public bool Accepted { get; private set; }

    public string Heading { get; } = title;
    public string Message { get; } = message;
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public string Accept { get; } = accept;
    public string Cancel { get; } = cancel ?? string.Empty;

    /// <summary>A question has two answers; a notice only its OK.</summary>
    public bool IsQuestion => cancel is not null;
    public bool IsNotice => !IsQuestion;

    [RelayCommand]
    Task AcceptAsync() => CloseWithAsync(true);

    [RelayCommand]
    Task CancelAsync() => CloseWithAsync(false);

    async Task CloseWithAsync(bool accepted)
    {
        Accepted = accepted;
        if (Popup is not null)
        {
            await Popup.CloseAsync();
        }
    }
}
