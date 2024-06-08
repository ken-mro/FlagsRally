using CountryData.Standard;
using FlagsRally.Models;
using FlagsRally.Utilities;
using System.Text;
using System.Text.Json;

namespace FlagsRally.Helpers;

public class SubRegionHelper
{
    private readonly CustomCountryHelper _countryHelper;
    private Dictionary<string, Dictionary<string, SubRegionCode>> _subRegionCodeMap = new();

    public SubRegionHelper(CustomCountryHelper countryHelper)
    {
        _countryHelper = countryHelper;

        var info = System.Reflection.Assembly.GetExecutingAssembly().GetName();
        using var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream($"{info.Name}.Resources.RegionCode.jp.json");
        using var streamReader = new StreamReader(stream!, Encoding.UTF8);
        string jsonString = streamReader.ReadToEnd();
        var jaJpSubRegionDataList = JsonSerializer.Deserialize<List<SubRegionData>>(jsonString);
        var jpSubRegionDict = jaJpSubRegionDataList?.ToDictionary(x => x.Name, x => new SubRegionCode(x.Code));

        _subRegionCodeMap.Add("JP", jpSubRegionDict!);
    }

    public bool GetLocalSubRegionNameIsSupported(string countryCode)
    {
        return _subRegionCodeMap.ContainsKey(countryCode.ToUpper());
    }

    private string GetLocalSubregionName(SubRegionCode subRegionCode)
    {
        if (!GetLocalSubRegionNameIsSupported(subRegionCode.CountryCode.ToUpper())) return string.Empty;
        var key = _subRegionCodeMap[subRegionCode.CountryCode].FirstOrDefault(x => x.Value == subRegionCode).Key;
        return key;
    }

    public List<SubRegion> GetBlankAllRegionList(Country countryInfo, string twoLetterCountryOrRegionName)
    {
        var upperdTwoLetterNmae = twoLetterCountryOrRegionName.ToUpper();
        var isCountryOfResidence = countryInfo.CountryShortCode.Equals(upperdTwoLetterNmae);
        var localNameIsSupported = GetLocalSubRegionNameIsSupported(upperdTwoLetterNmae);

        var blankAllSubregionList = _countryHelper.GetDistinctCountryRegionsBy(countryInfo.CountryShortCode)
            .OrderBy(x => x.ShortCode).Select(x =>
            {
                var code = new SubRegionCode(countryInfo.CountryShortCode, x.ShortCode);
                var name = GetLocalSubregionName(code);
                return new SubRegion
                {
                    Name = isCountryOfResidence && localNameIsSupported ?
                       GetLocalSubregionName(code) : x.Name,
                    Code = code
                };
            }).ToList();
        return blankAllSubregionList;
    }
}
