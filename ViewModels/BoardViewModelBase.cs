using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlagsRally.Helpers;
using FlagsRally.Resources;

namespace FlagsRally.ViewModels;

/// <summary>
/// What the two board pages (a country's regions and a stamp trail) share: the All / Visited /
/// Not visited switch with its counts and the progress, hiding the dates, pull to refresh and the
/// number of columns.
/// </summary>
public abstract partial class BoardViewModelBase : BaseViewModel, IVisitFilterable
{
    [ObservableProperty]
    int _gridItemSpan = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsAll))]
    [NotifyPropertyChangedFor(nameof(ShowsVisited))]
    [NotifyPropertyChangedFor(nameof(ShowsUnvisited))]
    VisitFilter _selectedVisitFilter = VisitFilter.All;

    public bool ShowsAll => SelectedVisitFilter == VisitFilter.All;
    public bool ShowsVisited => SelectedVisitFilter == VisitFilter.Visited;
    public bool ShowsUnvisited => SelectedVisitFilter == VisitFilter.Unvisited;

    public abstract int VisitedCount { get; }
    public abstract int TotalCount { get; }
    public int UnvisitedCount => TotalCount - VisitedCount;
    public double Progress => VisitProgress.Ratio(VisitedCount, TotalCount);
    public string ProgressText => VisitProgress.Text(VisitedCount, TotalCount);

    [RelayCommand]
    void SetVisitFilter(string filter)
    {
        SelectedVisitFilter = Enum.Parse<VisitFilter>(filter);
    }

    partial void OnSelectedVisitFilterChanged(VisitFilter value) => OnShownPlacesChanged();

    /// <summary>The places on the page or the filter changed: raise the lists the page shows.</summary>
    protected abstract void OnShownPlacesChanged();

    /// <summary>The board's places changed: the counts and the progress follow.</summary>
    protected void OnCountsChanged()
    {
        OnPropertyChanged(nameof(VisitedCount));
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(UnvisitedCount));
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(ProgressText));
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DateIsVisible))]
    bool _dateIsNotVisible;

    public bool DateIsVisible => !DateIsNotVisible;

    [ObservableProperty]
    bool _isRefreshing = false;

    public abstract Task Init();

    [RelayCommand]
    public async Task RefreshCountriesAsync()
    {
        IsRefreshing = true;
        await Init();
        IsRefreshing = false;
    }

    // "Hide date and month" as a small menu, ticked when on.
    [RelayCommand]
    async Task ChooseDisplayOptionsAsync()
    {
        var hideDate = ActionSheetLabel.Ticked(DateIsNotVisible, AppResources.HideDateAndMonth);
        var choice = await Shell.Current.DisplayActionSheetAsync(AppResources.DisplayOptions, AppResources.Cancel, null, hideDate);
        if (choice == hideDate)
        {
            DateIsNotVisible = !DateIsNotVisible;
        }
    }
}
