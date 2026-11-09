using System.Globalization;

namespace FlagsRally.Converters;

/// <summary>
/// The count shown after a label, such as "Not visited 225": values are (bool shown, int count).
/// It starts with an en space, where the line may break, so a count that does not fit goes to the
/// next line instead of being cut. Empty when the count is not shown.
/// </summary>
public class CountSuffixConverter : IMultiValueConverter
{
    public const string Separator = " ";

    public static string Format(bool shown, int count) =>
        shown ? Separator + count.ToString(CultureInfo.CurrentCulture) : string.Empty;

    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture) =>
        Format(values.Length > 0 && values[0] is true, values.Length > 1 && values[1] is int count ? count : 0);

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
