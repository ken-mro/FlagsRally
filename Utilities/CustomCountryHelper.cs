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

    public List<Regions> GetDistinctCountryRegionsBy(string countryCode)
    {
        return GetRegionByCountryCode(countryCode).GroupBy(x => x.ShortCode).Select(x => x.First()).ToList();
    }

    private void UpdateDe()
    {
        var CountryRegion = GetRegionByCountryCode("DE");
        var region = GetRegionByCountryCode("DE").FirstOrDefault(x => x.Name == "Bayern");
        CountryRegion.Add(new Regions(){ Name = "Bavaria", ShortCode = region?.ShortCode });

        region = GetRegionByCountryCode("DE").FirstOrDefault(x => x.Name == "Niedersachsen");
        CountryRegion.Add(new Regions(){ Name = "Lower Saxony", ShortCode = region?.ShortCode });

        region = GetRegionByCountryCode("DE").FirstOrDefault(x => x.Name == "Nordrhein-Westfalen");
        CountryRegion.Add(new Regions(){ Name = "North Rhine-Westphalia", ShortCode = region?.ShortCode });

        region = GetRegionByCountryCode("DE").FirstOrDefault(x => x.Name == "Rheinland-Pfalz");
        CountryRegion.Add(new Regions(){ Name = "Rhineland-Palatinate", ShortCode = region?.ShortCode });

        region = GetRegionByCountryCode("DE").FirstOrDefault(x => x.Name == "Sachsen");
        CountryRegion.Add(new Regions() { Name = "Saxony", ShortCode = region?.ShortCode });

        region = GetRegionByCountryCode("DE").FirstOrDefault(x => x.Name == "Sachsen-Anhalt");
        CountryRegion.Add(new Regions() { Name = "Saxony-Anhalt", ShortCode = region?.ShortCode });

        region = GetRegionByCountryCode("DE").FirstOrDefault(x => x.Name == "Thüringen");
        CountryRegion.Add(new Regions() { Name = "Thuringia", ShortCode = region?.ShortCode });

        GetCountryByCode("DE").Regions = CountryRegion.ToList();
    }

    private void UpdateUs()
    {
        var regions = GetCountryByCode("US").Regions;
        GetCountryByCode("US").Regions = regions.Where(x => !new[] { "AA", "AE", "AP", "AS", "FM", "GU", "MH", "MP", "PR", "PW", "VI" }
                        .Contains(x.ShortCode)).ToList();
    }

    private void UpdateIt()
    {
        var CountryRegion = GetRegionByCountryCode("IT");

        var region = GetRegionByCountryCode("IT").FirstOrDefault(x => x.Name == "Valle d'Aosta");
        CountryRegion.Add(new Regions() { Name = "Aosta Valley", ShortCode = region?.ShortCode });

        region = GetRegionByCountryCode("IT").FirstOrDefault(x => x.Name == "Piemonte");
        CountryRegion.Add(new Regions() { Name = "Piedmont", ShortCode = region?.ShortCode });

        region = GetRegionByCountryCode("IT").FirstOrDefault(x => x.Name == "Lombardia");
        CountryRegion.Add(new Regions() { Name = "Lombardy", ShortCode = region?.ShortCode });

        region = GetRegionByCountryCode("IT").FirstOrDefault(x => x.Name == "Trentino-Alto Adige");
        CountryRegion.Add(new Regions() { Name = "Trentino-South Tyrol", ShortCode = region?.ShortCode });

        region = GetRegionByCountryCode("IT").FirstOrDefault(x => x.Name == "Toscana");
        CountryRegion.Add(new Regions() { Name = "Tuscany", ShortCode = region?.ShortCode });
          
        region = GetRegionByCountryCode("IT").FirstOrDefault(x => x.Name == "Puglia");
        CountryRegion.Add(new Regions() { Name = "Apulia", ShortCode = region?.ShortCode });

        region = GetRegionByCountryCode("IT").FirstOrDefault(x => x.Name == "Sicilia");
        CountryRegion.Add(new Regions() { Name = "Sicily", ShortCode = region?.ShortCode });

        region = GetRegionByCountryCode("IT").FirstOrDefault(x => x.Name == "Sardegna");
        CountryRegion.Add(new Regions() { Name = "Sardinia", ShortCode = region?.ShortCode });

        GetCountryByCode("IT").Regions = CountryRegion.ToList();
    }
}
