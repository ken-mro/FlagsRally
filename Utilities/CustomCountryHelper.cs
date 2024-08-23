using CountryData.Standard;

namespace FlagsRally.Utilities;

public class CustomCountryHelper : CountryHelper
{
    public CustomCountryHelper() : base()
    {
        UpdateUs();
        UpdateDe();
        UpdateIt();
        UpdateFr();
        UpdateEs();
        UpdateMy();
        UpdateNo();
        UpdatePt();
        UpdateAt();
    }

    private void UpdateAt()
    {
        var CountryRegion = GetRegionByCountryCode("AT").ConvertAll(x =>
        {
            x.Name = x.Name.Replace("(", "").Replace(")", "");
            return x;
        });

        CountryRegion.Add(new Regions() { Name = "Tyrol", ShortCode = "7" });

        GetCountryByCode("AT").Regions = CountryRegion;
    }

    private void UpdatePt()
    {
        var CountryRegion = GetRegionByCountryCode("PT").ConvertAll(x =>
        {
            x.Name = x.Name.Replace("(", "").Replace(")", "");
            return x;
        });

        CountryRegion.Add(new Regions() { Name = "Aveiro District", ShortCode = "01" });
        CountryRegion.Add(new Regions() { Name = "Beja District", ShortCode = "02" });

        CountryRegion.Add(new Regions() { Name = "Coimbra District", ShortCode = "06" });

        CountryRegion.Add(new Regions() { Name = "Distrito de Évora", ShortCode = "07" });
        CountryRegion.Add(new Regions() { Name = "Évora District", ShortCode = "07" });

        CountryRegion.Add(new Regions() { Name = "Distrito da Guarda", ShortCode = "09" });
        CountryRegion.Add(new Regions() { Name = "Guarda District", ShortCode = "09" });

        CountryRegion.Add(new Regions() { Name = "Leiria District", ShortCode = "10" });

        GetCountryByCode("PT").Regions = CountryRegion;
    }

    private void UpdateNo()
    {
        List<string> existingPrefecturesList = new List<string>() { "03", "11", "15", "18", "31", "32", "33", "34", "39", "40", "42", "46", "50", "55", "56" };

        var countryRegion = GetRegionByCountryCode("NO").Where(x => existingPrefecturesList.Contains(x.ShortCode)).ToList();
        countryRegion.AddRange(
            new List<Regions>() {
                new Regions() { Name = "Østfold", ShortCode = "31" },
                new Regions() { Name = "Akershus", ShortCode = "32" },
                new Regions() { Name = "Buskerud", ShortCode = "33" },
                new Regions() { Name = "Innlandet", ShortCode = "34" },
                new Regions() { Name = "Vestfold", ShortCode = "39" },
                new Regions() { Name = "Telemark", ShortCode = "40" },
                new Regions() { Name = "Agder", ShortCode = "42" },
                new Regions() { Name = "Vestland", ShortCode = "46" },
                new Regions() { Name = "Trøndelag", ShortCode = "50" },
                new Regions() { Name = "Troms", ShortCode = "55" },
                new Regions() { Name = "Finnmark", ShortCode = "56" }
            }
        );

        GetCountryByCode("NO").Regions = countryRegion;
    }

    private void UpdateMy()
    {
        var CountryRegion = GetRegionByCountryCode("MY").ConvertAll(x =>
        {
            x.Name = x.Name.Replace("(", "").Replace(")", "");
            return x;
        });
        CountryRegion.Add(new Regions() { Name = "Penang", ShortCode = "07" });

        CountryRegion.Add(new Regions() { Name = "Federal Territory of Kuala Lumpur", ShortCode = "14" });
        CountryRegion.Add(new Regions() { Name = "Kuala Lumpur Federal Territory", ShortCode = "14" }); // Not encountered yet.
        CountryRegion.Add(new Regions() { Name = "Kuala Lumpur", ShortCode = "14" }); // Not encountered yet.

        CountryRegion.Add(new Regions() { Name = "Federal Territory of Labuan", ShortCode = "15" });
        CountryRegion.Add(new Regions() { Name = "Labuan Federal Territory", ShortCode = "15" }); // Not encountered yet.
        CountryRegion.Add(new Regions() { Name = "Labuan", ShortCode = "15" }); // Not encountered yet.

        CountryRegion.Add(new Regions() { Name = "Federal Territory of Putrajaya", ShortCode = "16" });
        CountryRegion.Add(new Regions() { Name = "Putrajaya Federal Territory", ShortCode = "16" }); // Not encountered yet.
        CountryRegion.Add(new Regions() { Name = "Putrajaya", ShortCode = "16" });
        GetCountryByCode("MY").Regions = CountryRegion;
    }

    public List<Regions> GetDistinctCountryRegionsBy(string countryCode)
    {
        return GetRegionByCountryCode(countryCode).GroupBy(x => x.ShortCode).Select(x => x.First()).ToList();
    }

    private void UpdateDe()
    {
        var CountryRegion = GetRegionByCountryCode("DE").ToList();
        var region = GetRegionByCountryCode("DE").FirstOrDefault(x => x.Name == "Bayern");
        CountryRegion.Add(new Regions() { Name = "Bavaria", ShortCode = region?.ShortCode });

        region = GetRegionByCountryCode("DE").FirstOrDefault(x => x.Name == "Niedersachsen");
        CountryRegion.Add(new Regions() { Name = "Lower Saxony", ShortCode = region?.ShortCode });

        region = GetRegionByCountryCode("DE").FirstOrDefault(x => x.Name == "Nordrhein-Westfalen");
        CountryRegion.Add(new Regions() { Name = "North Rhine-Westphalia", ShortCode = region?.ShortCode });

        region = GetRegionByCountryCode("DE").FirstOrDefault(x => x.Name == "Rheinland-Pfalz");
        CountryRegion.Add(new Regions() { Name = "Rhineland-Palatinate", ShortCode = region?.ShortCode });

        region = GetRegionByCountryCode("DE").FirstOrDefault(x => x.Name == "Sachsen");
        CountryRegion.Add(new Regions() { Name = "Saxony", ShortCode = region?.ShortCode });

        region = GetRegionByCountryCode("DE").FirstOrDefault(x => x.Name == "Sachsen-Anhalt");
        CountryRegion.Add(new Regions() { Name = "Saxony-Anhalt", ShortCode = region?.ShortCode });

        region = GetRegionByCountryCode("DE").FirstOrDefault(x => x.Name == "Thüringen");
        CountryRegion.Add(new Regions() { Name = "Thuringia", ShortCode = region?.ShortCode });

        GetCountryByCode("DE").Regions = CountryRegion;
    }

    private void UpdateUs()
    {
        var regions = GetCountryByCode("US").Regions;
        GetCountryByCode("US").Regions = regions.Where(x => !new[] { "AA", "AE", "AP", "AS", "FM", "GU", "MH", "MP", "PR", "PW", "VI" }
                        .Contains(x.ShortCode)).ToList();
    }

    private void UpdateIt()
    {
        var CountryRegion = GetRegionByCountryCode("IT").ToList();

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

        GetCountryByCode("IT").Regions = CountryRegion;
    }

    private void UpdateFr()
    {
        var CountryRegion = GetCountryByCode("FR").Regions.Where(x => x.ShortCode.Length == 3).ToList();

        var region = GetRegionByCountryCode("FR").FirstOrDefault(x => x.Name == "Provence-Alpes-Cote d'Azur");
        CountryRegion.Add(new Regions() { Name = "Provence-Alpes-Côte d'Azur", ShortCode = region?.ShortCode });

        region = GetRegionByCountryCode("FR").FirstOrDefault(x => x.Name == "Normandie");
        CountryRegion.Add(new Regions() { Name = "Normandy", ShortCode = region?.ShortCode });

        region = GetRegionByCountryCode("FR").FirstOrDefault(x => x.Name == "Corse");
        CountryRegion.Add(new Regions() { Name = "Corsica", ShortCode = region?.ShortCode });

        region = GetRegionByCountryCode("FR").FirstOrDefault(x => x.Name == "Bretagne");
        CountryRegion.Add(new Regions() { Name = "Brittany", ShortCode = region?.ShortCode });

        GetCountryByCode("FR").Regions = CountryRegion;
    }

    private void UpdateEs()
    {
        List<Regions> CountryRegion = new List<Regions>();

        CountryRegion.Add(new Regions() { Name = "Andalusia", ShortCode = "AN" });
        CountryRegion.Add(new Regions() { Name = "Aragon", ShortCode = "AR" });
        CountryRegion.Add(new Regions() { Name = "Principality of Asturias", ShortCode = "AS" });
        CountryRegion.Add(new Regions() { Name = "Canary Islands", ShortCode = "CN" });
        CountryRegion.Add(new Regions() { Name = "Cantabria", ShortCode = "CB" });
        CountryRegion.Add(new Regions() { Name = "Castile and León", ShortCode = "CL" });
        CountryRegion.Add(new Regions() { Name = "Castile-La Mancha", ShortCode = "CM" });
        CountryRegion.Add(new Regions() { Name = "Catalonia", ShortCode = "CT" });
        CountryRegion.Add(new Regions() { Name = "Ceuta", ShortCode = "CE" });
        CountryRegion.Add(new Regions() { Name = "Extremadura", ShortCode = "EX" });
        CountryRegion.Add(new Regions() { Name = "Galicia", ShortCode = "GA" });
        CountryRegion.Add(new Regions() { Name = "Balearic Islands", ShortCode = "IB" });
        CountryRegion.Add(new Regions() { Name = "La Rioja", ShortCode = "RI" });
        CountryRegion.Add(new Regions() { Name = "Community of Madrid", ShortCode = "MD" });
        CountryRegion.Add(new Regions() { Name = "Melilla", ShortCode = "ML" });
        CountryRegion.Add(new Regions() { Name = "Region of Murcia", ShortCode = "MC" });
        CountryRegion.Add(new Regions() { Name = "Chartered Community of Navarre", ShortCode = "NC" });
        CountryRegion.Add(new Regions() { Name = "Basque Country", ShortCode = "PV" });
        CountryRegion.Add(new Regions() { Name = "Valencian Community", ShortCode = "VC" });

        GetCountryByCode("ES").Regions = CountryRegion;
    }

    public string GetAdminAreaCode(string countryCode, string adminAreaName, string adminAreaShortName)
    {
        if (string.IsNullOrEmpty(countryCode)) return string.Empty;

        if (!adminAreaName.Equals(adminAreaShortName)
            && !string.IsNullOrEmpty(adminAreaShortName)) return adminAreaShortName;

        var countryRegions = GetCountryByCode(countryCode).Regions;
        return countryRegions?.Where(x => x.Name == adminAreaName)?.FirstOrDefault()?.ShortCode ?? string.Empty;
    }

    public string GetAdminAreaCode(string countryCode, string adminAreaName)
    {
        if (string.IsNullOrEmpty(countryCode)) return string.Empty;

        var countryRegions = GetCountryByCode(countryCode).Regions;
        return countryRegions?.Where(x => x.Name == adminAreaName)?.FirstOrDefault()?.ShortCode ?? string.Empty;
    }

    private List<int> GetNumberList()
    {
        return new List<int>()
            {
                3, 11, 15, 18, 31, 32, 33, 34, 39, 40, 42, 46, 50, 55, 56
            };
    }
}
