using FlagsRally.Models;

namespace FlagsRally.Views;

/// <summary>
/// Picks a passport stamp's template: the original square, or the one for the other patterns,
/// whose outline <see cref="Controls.StampFrame"/> draws. Each holds only the views it shows.
/// </summary>
public class StampTemplateSelector : DataTemplateSelector
{
    public DataTemplate SquareStamp { get; set; } = null!;
    public DataTemplate ShapedStamp { get; set; } = null!;

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container) =>
        item is ArrivalLocation { StampPattern: not StampPattern.Square } ? ShapedStamp : SquareStamp;
}
