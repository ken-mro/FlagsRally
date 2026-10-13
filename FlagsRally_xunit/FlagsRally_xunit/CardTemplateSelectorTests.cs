using FlagsRally.Models;
using FlagsRally.ViewModels;
using FlagsRally.Views;
using Microsoft.Maui.Controls;

namespace FlagsRallyTests.Views;

// Each kind of card or row gets the template holding only its own views; a wrong pick would show
// an empty card, or bring back loading pictures that are not shown.
public class CardTemplateSelectorTests
{
    static readonly BindableObject Container = new Label();

    [Theory]
    [InlineData(CollectionCardKind.Add, "", "Add")]
    [InlineData(CollectionCardKind.Custom, "https://example.com/stamp.png", "Remote")]
    [InlineData(CollectionCardKind.Regional, "jp_13_emblem.png", "Local")]
    [InlineData(CollectionCardKind.Regional, "", "Badge")]
    [InlineData(CollectionCardKind.Custom, "", "Badge")]
    public void A_collection_card_gets_the_template_for_what_it_shows(CollectionCardKind kind, string imageUrl, string expected)
    {
        var templates = new Dictionary<string, DataTemplate>
        {
            ["Add"] = new(), ["Remote"] = new(), ["Local"] = new(), ["Badge"] = new(),
        };
        var selector = new CollectionCardTemplateSelector
        {
            AddCard = templates["Add"],
            RemoteImageCard = templates["Remote"],
            LocalImageCard = templates["Local"],
            BadgeCard = templates["Badge"],
        };
        var card = new CollectionCard(kind, "key", "Title", 0, 1, imageUrl, "B");

        Assert.Same(templates[expected], selector.SelectTemplate(card, Container));
    }

    [Fact]
    public void A_region_tile_is_stamped_only_once_visited()
    {
        var selector = new RegionTileTemplateSelector { Visited = new DataTemplate(), NotVisited = new DataTemplate() };
        var visited = new SubRegion { Code = new SubRegionCode("JP", "13"), ArrivalDate = new DateTime(2026, 10, 1) };
        var notVisited = new SubRegion { Code = new SubRegionCode("JP", "01") };

        Assert.Same(selector.Visited, selector.SelectTemplate(visited, Container));
        Assert.Same(selector.NotVisited, selector.SelectTemplate(notVisited, Container));
    }

    [Fact]
    public void A_catalog_folder_and_board_get_their_own_rows()
    {
        var selector = new BoardCatalogRowTemplateSelector { Folder = new DataTemplate(), Board = new DataTemplate() };
        var folder = new BoardCatalogItem(new FlagsRally.Services.DriveEntry("1", "マンホールカード", IsFolder: true));
        var board = new BoardCatalogItem(new FlagsRally.Services.DriveEntry("2", "board.json", IsFolder: false));

        Assert.Same(selector.Folder, selector.SelectTemplate(folder, Container));
        Assert.Same(selector.Board, selector.SelectTemplate(board, Container));
    }
}
