using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FlagsRally.Converters
{
    public static class StringExtensions
    {
        public static bool ContainsJapaneseCharacters(this string text)
        {
            return Regex.IsMatch(text, @"[\p{IsHiragana}\p{IsKatakana}\p{IsCJKUnifiedIdeographs}]+");
        }
    }
}
