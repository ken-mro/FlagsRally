using FlagsRally.ViewModels;

namespace FlagsRally.Views;

/// <summary>
/// Picks the card template for what the card shows: a stamp from the web, a bundled emblem,
/// the badge drawn when there is no stamp yet, or the "Add board" card.
/// </summary>
public class CollectionCardTemplateSelector : DataTemplateSelector
{
    public DataTemplate RemoteImageCard { get; set; } = null!;
    public DataTemplate LocalImageCard { get; set; } = null!;
    public DataTemplate BadgeCard { get; set; } = null!;
    public DataTemplate AddCard { get; set; } = null!;

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container) => item switch
    {
        CollectionCard { IsAddCard: true } => AddCard,
        CollectionCard { HasRemoteImage: true } => RemoteImageCard,
        CollectionCard { HasLocalImage: true } => LocalImageCard,
        _ => BadgeCard,
    };
}
