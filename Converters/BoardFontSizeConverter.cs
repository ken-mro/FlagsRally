using System.Globalization;

namespace FlagsRally.Converters
{
    public class BoardFontSizeConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string? text = value as string;
            if (string.IsNullOrEmpty(text)) return 20;

            var minValue = text.ContainsJapaneseCharacters() ?
                Math.Min(20, 20 * 8 / text.Length) : Math.Min(20, 20 * 12 / text.Length);

            return Math.Max(15, minValue);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return 20;
        }
    }
}