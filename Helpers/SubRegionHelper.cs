using CountryData.Standard;
using FlagsRally.Models;
using System.Text;
using System.Text.Json;

namespace FlagsRally.Helpers;

public class SubRegionHelper
{
    private readonly CountryHelper _countryHelper;
    private Dictionary<string, Dictionary<string, SubRegionCode>> _subRegionCodeMap = new();

    public SubRegionHelper(CountryHelper countryHelper)
    {
        _countryHelper = countryHelper;

        var info = System.Reflection.Assembly.GetExecutingAssembly().GetName();
        using var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream($"{info.Name}.Resources.RegionCode.jp.json");
        using var streamReader = new StreamReader(stream!, Encoding.UTF8);
        string jsonString = streamReader.ReadToEnd();
        var jaJpSubRegionDataList = JsonSerializer.Deserialize<List<SubRegionData>>(jsonString);
        var enJpSubRegionDataList = _countryHelper?.GetCountryByCode("JP").Regions.ConvertAll(x => new SubRegionData(x.Name, "JP-" + x.ShortCode));
        jaJpSubRegionDataList?.AddRange(enJpSubRegionDataList!);

        var jpSubRegionDict = jaJpSubRegionDataList?.ToDictionary(x => x.Name, x => new SubRegionCode(x.Code));
        var usSubRegionDict = _countryHelper?.GetCountryByCode("US").Regions
            .Where(x => !new[] { "AA", "AE", "AP", "AS", "FM", "GU", "MH", "MP", "PR", "PW", "VI" }.Contains(x.ShortCode)).ToList()
            .ToDictionary(x => x.Name, x => new SubRegionCode("US", x.ShortCode));

        _subRegionCodeMap.Add("JP", jpSubRegionDict!);
        _subRegionCodeMap.Add("US", usSubRegionDict!);
    }

    public string GetJaSubregionName(SubRegionCode subRegionCode)
    {
        var key = _subRegionCodeMap["JP"].FirstOrDefault(x => x.Value == subRegionCode).Key;
        return key;
    }
}
