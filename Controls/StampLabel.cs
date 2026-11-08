using FlagsRally.Converters;

namespace FlagsRally.Controls;

/// <summary>
/// Label drawn in the stamp typefaces: JerseyclubGrunge for Latin text and
/// Kaisei Tokumin for Japanese text (JerseyclubGrunge has no Japanese glyphs).
/// </summary>
public class StampLabel : Label
{
    const string LatinFont = "JerseyclubGrungeBold";
    const string JapaneseFont = "KaiseiTokumin";

    public StampLabel()
    {
        FontFamily = LatinFont;
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == TextProperty.PropertyName)
        {
            FontFamily = !string.IsNullOrEmpty(Text) && Text.ContainsJapaneseCharacters() ? JapaneseFont : LatinFont;
        }
    }
}
