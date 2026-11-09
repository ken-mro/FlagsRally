using System.Globalization;

namespace FlagsRally.Converters;

/// <summary>
/// Picks <see cref="TrueColor"/> or <see cref="FalseColor"/> from a bool. Used where a trigger cannot
/// be set, such as on a <see cref="Span"/>.
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public Color TrueColor { get; set; } = Colors.Black;
    public Color FalseColor { get; set; } = Colors.Black;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? TrueColor : FalseColor;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
