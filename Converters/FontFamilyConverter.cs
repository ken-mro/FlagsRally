using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace FlagsRally.Converters
{
    public class FontFamilyConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string fontFamily = "JerseyclubGrungeBold";
            string? text = value as string;

            if (!string.IsNullOrEmpty(text) && text.ContainsJapaneseCharacters())
            {
                fontFamily = "craftmincho";
            }

            return fontFamily;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return "JerseyclubGrungeBold";
        }
    }
}
