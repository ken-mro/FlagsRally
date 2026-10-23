using FlagsRally.Models;

namespace FlagsRally.Views;

/// <summary>
/// Picks a passport stamp's template: the original square, the journey picture, or the one for the
/// other patterns, whose outline <see cref="Controls.StampFrame"/> draws. Each holds only the views it shows.
/// </summary>
public class StampTemplateSelector : DataTemplateSelector
{
    public DataTemplate SquareStamp { get; set; } = null!;
    public DataTemplate ShapedStamp { get; set; } = null!;
    public DataTemplate JourneyStamp { get; set; } = null!;

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container) => item switch
    {
        ArrivalLocation { StampPattern: StampPattern.Journey } => JourneyStamp,
        ArrivalLocation { StampPattern: not StampPattern.Square } => ShapedStamp,
        _ => SquareStamp,
    };
}
