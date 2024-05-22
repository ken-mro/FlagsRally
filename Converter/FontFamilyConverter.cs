using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace FlagsRally.Converters
{
    public class FontFamilyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string fontFamily = "JerseyclubGrungeBold";
            string text = value as string;

            if (!string.IsNullOrEmpty(text) && text.ContainsJapaneseCharacters())
            {
                fontFamily = "KouzanMouhituFontOTF";
            }

            return fontFamily;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public static class StringExtensions
    {
        public static bool ContainsJapaneseCharacters(this string text)
        {
            // Check if the text contains any Japanese characters
            // You can implement your own logic here
            // This is just a placeholder implementation
            return Regex.IsMatch(text, @"[\p{IsHiragana}\p{IsKatakana}\p{IsCJKUnifiedIdeographs}]+");
        }
    }
}
