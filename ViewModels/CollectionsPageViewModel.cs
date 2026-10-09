using CommunityToolkit.Mvvm.Input;
using FlagsRally.Repository;
using FlagsRally.Resources;
using FlagsRally.Services;
using FlagsRally.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace FlagsRally.ViewModels;

public enum CollectionCardKind { Regional, Custom, Add }

/// <summary>
/// A card on the Collections page: a country's regional flags, a custom board, or the "Add board" card.
/// </summary>
/// <param name="Key">Country code for a regional card, board name for a custom one.</param>
/// <param name="ImageUrl">Latest stamp: a URL or a bundled image file name.</param>
/// <param name="Badge">Shown when there is no stamp yet (country flag or the board's initial).</param>
public record CollectionCard(CollectionCardKind Kind, string Key, string Title, int Visited, int Total, string ImageUrl, string Badge)
{
    public bool IsAddCard => Kind == CollectionCardKind.Add;
    public bool IsBoard => !IsAddCard;
    public bool HasImage => !string.IsNullOrEmpty(ImageUrl);
    public bool HasRemoteImage => HasImage && ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase);
    public bool HasLocalImage => HasImage && !HasRemoteImage;
    public bool HasNoImage => IsBoard && !HasImage;
    // Each image view gets only its own kind of source, so a recycled card never shows another card's picture.
    public string? RemoteImageUrl => HasRemoteImage ? ImageUrl : null;
    public string? LocalImageFile => HasLocalImage ? ImageUrl : null;
    public double Progress => Total == 0 ? 0 : (double)Visited / Total;
    public string ProgressText => $"{Visited} / {Total}";
}

/// <summary>
/// A titled group of cards; only the custom boards can be edited. A collapsible group can be
/// folded down to its heading: it then holds no items, while <see cref="Cards"/> keeps them all.
/// </summary>
public class CollectionSection(string title, bool isEditable, bool isCollapsible = false) : ObservableCollection<CollectionCard>
{
    List<CollectionCard> _cards = [];
    bool _isExpanded = true;
    string _summary = string.Empty;

    public string Title { get; } = title;
    public bool IsEditable { get; } = isEditable;
    public bool IsCollapsible { get; } = isCollapsible;

    public IReadOnlyList<CollectionCard> Cards => _cards;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            ShowCards();
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsExpanded)));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Chevron)));
        }
    }

    public string Chevron => IsExpanded ? "▾" : "▸";

    /// <summary>
    /// Shown beside the heading, e.g. how many countries have stamps.
    /// </summary>
    public string Summary
    {
        get => _summary;
        set
        {
            _summary = value;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Summary)));
        }
    }

    public void SetCards(IEnumerable<CollectionCard> cards)
    {
        _cards = cards.ToList();
        ShowCards();
    }

    void ShowCards()
    {
        Clear();
        if (!IsExpanded) return;
        foreach (var card in _cards)
        {
            Add(card);
        }
    }
}

public partial class CollectionsPageViewModel : BaseViewModel
{
    readonly ICustomBoardRepository _customBoardRepository;
    readonly ICustomLocationDataRepository _customLocationDataRepository;
    readonly RegionalFlagsService _regionalFlagsService;
    readonly SettingsPreferences _settingsPreferences;

    public CollectionsPageViewModel(ICustomBoardRepository customBoardRepository, ICustomLocationDataRepository customLocationDataRepository, RegionalFlagsService regionalFlagsService, SettingsPreferences settingsPreferences)
    {
        _customBoardRepository = customBoardRepository;
        _customLocationDataRepository = customLocationDataRepository;
        _regionalFlagsService = regionalFlagsService;
        _settingsPreferences = settingsPreferences;
        RegionalCards.IsExpanded = settingsPreferences.GetRegionalFlagsExpanded();
        Sections = [RegionalCards, BoardCards];
    }

    // The long list of countries can be folded away to get to the custom boards.
    public CollectionSection RegionalCards { get; } = new(AppResources.CountriesAndRegions, isEditable: false, isCollapsible: true);

    public CollectionSection BoardCards { get; } = new(AppResources.CustomBoards, isEditable: true);

    public ObservableCollection<CollectionSection> Sections { get; }

    public async Task Init()
    {
        try
        {
            var boards = await _customBoardRepository.GetAllCustomBoards();
            var locationsByBoard = (await _customLocationDataRepository.GetAllCustomLocations())
                .GroupBy(l => l.Board.Name)
                .ToDictionary(g => g.Key, g => g.ToList());

            await LoadRegionalCardsAsync();

            BoardCards.Clear();
            foreach (var board in boards)
            {
                var locations = locationsByBoard.GetValueOrDefault(board.Name) ?? [];
                var latestVisit = locations.Where(l => l.HasBeenVisited).MaxBy(l => l.ArrivalDate);
                BoardCards.Add(new CollectionCard(
                    CollectionCardKind.Custom,
                    board.Name,
                    board.Name,
                    locations.Count(l => l.HasBeenVisited),
                    locations.Count,
                    latestVisit?.ImageUrl ?? string.Empty,
                    string.IsNullOrEmpty(board.Name) ? string.Empty : board.Name[..1]));
            }
            BoardCards.Add(new CollectionCard(CollectionCardKind.Add, string.Empty, AppResources.AddBoard, 0, 0, string.Empty, string.Empty));
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync($"{AppResources.Error}", ex.Message, "OK");
        }
    }

    // Countries with stamps come first, keeping the service's order (latest country first) otherwise.
    private async Task LoadRegionalCardsAsync()
    {
        var cards = new List<CollectionCard>();
        foreach (var country in _regionalFlagsService.GetCountries())
        {
            var regions = await _regionalFlagsService.GetRegionsAsync(country);
            var latestVisit = regions.Where(r => r.HasBeenVisited).MaxBy(r => r.ArrivalDate);
            cards.Add(new CollectionCard(
                CollectionCardKind.Regional,
                country.CountryShortCode,
                country.CountryName,
                regions.Count(r => r.HasBeenVisited),
                regions.Count,
                latestVisit?.FlagSource ?? string.Empty,
                country.CountryFlag));
        }

        RegionalCards.SetCards(cards.OrderBy(c => c.Visited == 0));
        RegionalCards.Summary = $"{cards.Count(c => c.Visited > 0)} / {cards.Count}";
    }

    [RelayCommand]
    void ToggleSection(CollectionSection section)
    {
        if (!section.IsCollapsible) return;

        section.IsExpanded = !section.IsExpanded;
        if (section == RegionalCards)
        {
            _settingsPreferences.SetRegionalFlagsExpanded(section.IsExpanded);
        }
    }

    [RelayCommand]
    async Task EditBoardsAsync() => await Shell.Current.GoToAsync(ManageCustomBoardsPage.Route);

    [RelayCommand]
    async Task OpenCardAsync(CollectionCard card)
    {
        switch (card.Kind)
        {
            case CollectionCardKind.Add:
                await AddBoardAsync();
                break;
            case CollectionCardKind.Regional:
                await Shell.Current.GoToAsync(FlagsBoardPage.Route, new ShellNavigationQueryParameters
                {
                    { FlagsBoardPageViewModel.CountryQueryKey, card.Key },
                });
                break;
            default:
                await Shell.Current.GoToAsync(CustomBoardPage.Route, new ShellNavigationQueryParameters
                {
                    { CustomBoardPageViewModel.BoardQueryKey, card.Key },
                });
                break;
        }
    }

    private static Task AddBoardAsync() => Shell.Current.GoToAsync(BoardCatalogPage.Route);
}
