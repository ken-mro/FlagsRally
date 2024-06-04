using CountryData.Standard;
using System.Diagnostics;

namespace FlagsRally.Utilities;

public class CustomCountryHelper : CountryHelper
{
    public CustomCountryHelper() : base()
    {
        UpdateUs();
        UpdateDe();
        UpdateIt();
    }

    private void UpdateDe()
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

    private void UpdateUs()
    {
        var regions = GetCountryByCode("US").Regions;
        GetCountryByCode("US").Regions = regions.Where(x => !new[] { "AA", "AE", "AP", "AS", "FM", "GU", "MH", "MP", "PR", "PW", "VI" }
                        .Contains(x.ShortCode)).ToList();
    }

    private void UpdateIt()
    {
        var italyRegions = GetCountryByCode("IT").Regions.ToList();
        foreach (var italyRegion in italyRegions)
        {
            Debug.WriteLine(italyRegion.Name, italyRegion.ShortCode);
        }

        var region = GetRegionByCountryCode("IT").FirstOrDefault(x => x.Name == "Valle d'Aosta");
        region!.Name = "Aosta Valley";

        region = GetRegionByCountryCode("IT").FirstOrDefault(x => x.Name == "Piemonte");
        region!.Name = "Piedmont";

        region = GetRegionByCountryCode("IT").FirstOrDefault(x => x.Name == "Lombardia");
        region!.Name = "Lombardy";

        region = GetRegionByCountryCode("IT").FirstOrDefault(x => x.Name == "Trentino-Alto Adige");
        region!.Name = "Trentino-South Tyrol";

        region = GetRegionByCountryCode("IT").FirstOrDefault(x => x.Name == "Toscana");
        region!.Name = "Tuscany";

        region = GetRegionByCountryCode("IT").FirstOrDefault(x => x.Name == "Puglia");
        region!.Name = "Apulia";

        region = GetRegionByCountryCode("IT").FirstOrDefault(x => x.Name == "Sicilia");
        region!.Name = "Sicily";

        region = GetRegionByCountryCode("IT").FirstOrDefault(x => x.Name == "Sardegna");
        region!.Name = "Sardinia";
    }
}
