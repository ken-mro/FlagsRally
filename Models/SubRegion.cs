using System.Globalization;
using FlagsRally.Repository;

namespace FlagsRally.Models;

public class SubRegion
{
    public int Id { get; init;}
    public DateTime ArrivalDate { get; set; }

    public string IsoCode => Code.lowerCountryCodeHyphenRegionCode.ToUpper();
    public string ArrivalDateString => ArrivalDate.ToString("dd  MMM  yyyy", CultureInfo.CreateSpecificCulture("en-US"));
    public string Name { get; init; } = string.Empty;
    public string EnAdminAreaName { get; init; } = string.Empty;
    public SubRegionCode Code { get; init; } = SubRegionCode.EmptyCode();
    public string FlagSource => GetFlagSource(Code);
    public string MapToolTipImageSource => GetToolTipImageSource();


    public bool HasBeenVisited => !(ArrivalDate == DateTime.MinValue);
    public bool HasNotBeenVisited => !HasBeenVisited;

    public static string GetFlagSource(SubRegionCode subRegionCode)
    {
        if (subRegionCode.lowerCountryCodeHyphenRegionCode[0..2] == "us")
        {
            if (subRegionCode.lowerCountryCodeHyphenRegionCode == "us-dc") return "us_dc.png";

            return $"https://flagcdn.com/160x120/{subRegionCode.lowerCountryCodeHyphenRegionCode}.png";
        }
        else if (Constants.SupportedSubRegionCountryCodeList.Contains(subRegionCode.lowerCountryCodeHyphenRegionCode[0..2]))
        {
            return $"{subRegionCode.ImageResourceString}_emblem.png";
        }
        else
        {
            throw new ArgumentException("Unexpected country's SubRegionCode");
        }
    }
        private string GetToolTipImageSource()
    {
        if (Code.lowerCountryCodeHyphenRegionCode[0..2] == "us")
        {
            if (Code.lowerCountryCodeHyphenRegionCode == "us-dc") return "us_dc.png";

            return $"https://flagcdn.com/160x120/{Code.lowerCountryCodeHyphenRegionCode}.png";
        }
        else if (Constants.SupportedSubRegionCountryCodeList.Contains(Code.lowerCountryCodeHyphenRegionCode[0..2]))
        {
            return $"{Code.ImageResourceString}.png";
        }
        else
        {
            throw new ArgumentException("Unexpected country's SubRegionCode");
        }
    }
}
