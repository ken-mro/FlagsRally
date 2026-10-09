using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlagsRally.Models.CustomBoard;
using System.ComponentModel;

namespace FlagsRally.ViewModels;

public partial class PinFilterOption : ObservableObject
{
    public PinFilterOption(CustomBoardPinFilterItem item, bool isSelected)
    {
        Name = item.Name;
        IsAll = item.IsAll;
        _isSelected = isSelected;
    }

    public string Name { get; }
    public bool IsAll { get; }

    [ObservableProperty]
    bool _isSelected;
}

public partial class PinFilterPopupViewModel : BaseViewModel
{
    bool _isNormalizing;

    public Popup? Popup { get; set; }

    public List<PinFilterOption> Options { get; }

    /// <summary>
    /// Set when the user taps OK. Empty means "show all pins"; null means the popup was cancelled or dismissed.
    /// </summary>
    public IReadOnlyCollection<string>? Result { get; private set; }

    /// <param name="items">All filter items; the "All pins" item is expected to be first.</param>
    /// <param name="selectedPinKeys">Currently visible pin keys; empty means all pins are shown.</param>
    public PinFilterPopupViewModel(IEnumerable<CustomBoardPinFilterItem> items, IReadOnlyCollection<string> selectedPinKeys)
    {
        var showsAll = selectedPinKeys.Count == 0;
        Options = items
            .Select(item => new PinFilterOption(item, item.IsAll ? showsAll : selectedPinKeys.Contains(item.Name)))
            .ToList();

        foreach (var option in Options)
        {
            option.PropertyChanged += OnOptionPropertyChanged;
        }
    }

    // "All pins" and individual items are mutually exclusive.
    private void OnOptionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isNormalizing || e.PropertyName != nameof(PinFilterOption.IsSelected)) return;
        if (sender is not PinFilterOption changed || !changed.IsSelected) return;

        _isNormalizing = true;
        foreach (var option in Options.Where(o => o != changed && (changed.IsAll || o.IsAll)))
        {
            option.IsSelected = false;
        }
        _isNormalizing = false;
    }

    [RelayCommand]
    async Task ConfirmAsync()
    {
        Result = Options.Any(o => o.IsAll && o.IsSelected)
            ? []
            : Options.Where(o => !o.IsAll && o.IsSelected).Select(o => o.Name).ToList();
        await CloseAsync();
    }

    [RelayCommand]
    async Task CancelAsync()
    {
        Result = null;
        await CloseAsync();
    }

    private async Task CloseAsync()
    {
        if (Popup is not null)
        {
            await Popup.CloseAsync();
        }
    }
}
