using FlagsRally.Models;

namespace FlagsRally.Views;

/// <summary>
/// Picks a region's tile on the regional flags board: the stamped one once visited, the plain one before.
/// </summary>
public class RegionTileTemplateSelector : DataTemplateSelector
{
    public DataTemplate Visited { get; set; } = null!;
    public DataTemplate NotVisited { get; set; } = null!;

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container) =>
        item is SubRegion { HasBeenVisited: true } ? Visited : NotVisited;
}
