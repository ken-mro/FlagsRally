using FlagsRally.ViewModels;

namespace FlagsRally.Views;

/// <summary>
/// Picks the row for an entry in Add Board: a folder to open or a board to tick.
/// </summary>
public class BoardCatalogRowTemplateSelector : DataTemplateSelector
{
    public DataTemplate Folder { get; set; } = null!;
    public DataTemplate Board { get; set; } = null!;

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container) =>
        item is BoardCatalogItem { IsFolder: true } ? Folder : Board;
}
