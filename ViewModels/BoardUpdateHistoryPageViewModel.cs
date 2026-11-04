using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FlagsRally.Resources;
using FlagsRally.Services;
using System.Globalization;
using System.Text;

namespace FlagsRally.ViewModels;

/// <summary>
/// One stamp trail in the update history, worded for display.
/// </summary>
public class BoardUpdateItem(BoardUpdateEntry entry)
{
    public string Name => entry.Name;
    public string Collection => entry.Collection;
    public bool HasCollection => entry.Collection.Length > 0;
    public bool IsUpdated => entry.IsUpdated;

    // Unknown actions are shown as written, so a new kind of line in the file still reads sensibly.
    public string ActionText =>
        entry.IsAdded ? AppResources.BoardUpdateAdded
        : entry.IsUpdated ? AppResources.BoardUpdateUpdated
        : entry.Action;
    public bool HasAction => ActionText.Length > 0;

    public string CountText => entry.Count is int count ? string.Format(AppResources.BoardUpdatePlaces, count) : "";
    public bool HasCount => entry.Count is not null;

    public string Note => entry.Note;
    public bool HasNote => entry.Note.Length > 0;
}

/// <summary>
/// The update history entries of one day.
/// </summary>
public class BoardUpdateDay(DateOnly date, IEnumerable<BoardUpdateItem> items) : List<BoardUpdateItem>(items)
{
    public string DateText { get; } = date.ToString("d", CultureInfo.CurrentCulture);
}

/// <summary>
/// Shows the update history CSV published in the shared board folder.
/// </summary>
public partial class BoardUpdateHistoryPageViewModel(DriveBoardCatalog catalog) : BaseViewModel, IQueryAttributable
{
    public const string FileIdQueryKey = "fileId";

    string? _fileId;
    bool _isLoaded;

    // Replaced as a whole, never edited in place: a grouped CollectionView on iOS
    // can crash when items are added right after a reset.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsEmpty))]
    List<BoardUpdateDay> _days = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsEmpty))]
    bool _hasLoadError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsEmpty))]
    bool _isLoading;

    public bool ShowsEmpty => !IsLoading && !HasLoadError && Days.Count == 0;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(FileIdQueryKey, out var id) && id is string fileId && fileId != _fileId)
        {
            _fileId = fileId;
            _isLoaded = false;
        }
    }

    public async Task Init()
    {
        if (_isLoaded) return;
        await LoadAsync();
    }

    [RelayCommand]
    Task RetryAsync() => LoadAsync();

    async Task LoadAsync()
    {
        if (IsLoading) return;

        HasLoadError = false;
        IsLoading = true;
        try
        {
            if (_fileId is null) throw new InvalidOperationException("No history file was given.");

            await using var stream = await catalog.DownloadAsync(_fileId);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var entries = BoardUpdateHistory.Parse(await reader.ReadToEndAsync());

            Days = entries
                .GroupBy(x => x.Date)
                .Select(day => new BoardUpdateDay(day.Key, day.Select(x => new BoardUpdateItem(x))))
                .ToList();
            _isLoaded = true;
        }
        catch (Exception)
        {
            Days = [];
            HasLoadError = true;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
