using FlagsRally.Helpers;

namespace FlagsRally.Models;

public class SubRegion
{
    public int Id { get; init;}
    public DateTime ArrivalDate { get; set; }

    public string IsoCode => Code.lowerCountryCodeHyphenRegionCode.ToUpper();
    public string ArrivalDateString => StampDate.FormatSpaced(ArrivalDate);
    public string Name { get; init; } = string.Empty;
    public string EnAdminAreaName { get; init; } = string.Empty;
    public SubRegionCode Code { get; init; } = SubRegionCode.EmptyCode();
    public string FlagSource => GetFlagSource(Code);
    public string MapToolTipImageSource => GetToolTipImageSource();


    /// <summary>The colours of its tile, after its country.</summary>
    public CountryTileStyle Tile => CountryTileStyles.For(Code.CountryCode);

    public bool HasBeenVisited => !(ArrivalDate == DateTime.MinValue);
    public bool HasNotBeenVisited => !HasBeenVisited;

    public static string GetFlagSource(SubRegionCode subRegionCode) => FlagSources.RegionFlag(subRegionCode, emblem: true);

    private string GetToolTipImageSource() => FlagSources.RegionFlag(Code);
}
