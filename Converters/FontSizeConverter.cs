using System.Globalization;

namespace FlagsRally.Converters
{
    public class FontSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = value as string;
            if (string.IsNullOrEmpty(text)) return 20;
            int ratio;
            if (text.ContainsJapaneseCharacters())
            {
                return Math.Min(20, 20 * (8 / text.Length));
            }
            else
            {
                return Math.Min(20, 20 * (23 / text.Length));
            } 
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return 20;
        }
    }
}