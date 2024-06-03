using CountryData.Standard;

namespace FlagsRally.Utilities;

public class CustomCountryHelper : CountryHelper
{
    public CustomCountryHelper() : base()
    {
        var region = GetRegionByCountryCode("DE").FirstOrDefault(x => x.Name == "Bayern");
        region!.Name = "Bavaria";

        region = GetRegionByCountryCode("DE").FirstOrDefault(x => x.Name == "Niedersachsen");
        region!.Name = "Lower Saxony";

        region = GetRegionByCountryCode("DE").FirstOrDefault(x => x.Name == "Nordrhein-Westfalen");
        region!.Name = "North Rhine-Westphalia";

        region = GetRegionByCountryCode("DE").FirstOrDefault(x => x.Name == "Rheinland-Pfalz");
        region!.Name = "Rhineland-Palatinate";

        region = GetRegionByCountryCode("DE").FirstOrDefault(x => x.Name == "Sachsen");
        region!.Name = "Saxony";

        region = GetRegionByCountryCode("DE").FirstOrDefault(x => x.Name == "Sachsen-Anhalt");
        region!.Name = "Saxony-Anhalt";

        region = GetRegionByCountryCode("DE").FirstOrDefault(x => x.Name == "Thüringen");
        region!.Name = "Thuringia";
    }
}
