using System.Globalization;

namespace FlagsRally.Models;

public class SubRegion
{
    public DateTime ArrivalDate { get; set; }

    public string ArrivalDateString => ArrivalDate.ToString("dd  MMM  yyyy", CultureInfo.CreateSpecificCulture("en-US"));
    public string Name { get; init; }

    public SubRegionCode Code { get; init; }
    public string FlagSource => GetFlagSource();
    public bool HasBeenVisited => !(ArrivalDate == DateTime.MinValue);
    public bool HasNotBeenVisited => !HasBeenVisited;

    private string GetFlagSource()
    {
        if (Code.lower5LetterRegionCode[0..2] == "us")
        {
            if (Code.lower5LetterRegionCode == "us-dc") return "us_dc.png";

            return $"https://flagcdn.com/160x120/{Code.lower5LetterRegionCode}.png";
        }
        else if (Code.lower5LetterRegionCode[0..2] == "jp")
        {
            return $"{Code.GetImageResourceString()}_emblem.png";
        }
        else
        {
            throw new ArgumentException("Unexpected country's SubRegionCode");
        }
    }
}
