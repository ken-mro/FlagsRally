using FlagsRally.Repository;

namespace FlagsRally.Models;

public record SubRegionCode
{
    public string lowerCountryCodeHyphenRegionCode => $"{CountryCode.ToLower()}-{RegionCode.ToLower()}";
    public string ImageResourceString => $"{CountryCode.ToLower()}_{RegionCode.ToLower()}";
    public string RegionCode { get; init; }
    public string CountryCode { get; init; }
    public SubRegionCode(string countryCodeHyphenSubRegionCode)
    {
        CountryCode = countryCodeHyphenSubRegionCode[0..2].ToUpper();
        if (!Constants.SupportedSubRegionCountryCodeList.Contains(CountryCode.ToLower())) throw new ArgumentException("Unsupported SubRegion's CountryCode");
        if (countryCodeHyphenSubRegionCode[2] != '-') throw new ArgumentException("SubRegionCode must have a hyphen in the middle");

        RegionCode = countryCodeHyphenSubRegionCode.Substring(3).ToUpper();
    }

    public SubRegionCode(string countryCode, string subRegionCode)
    {
        var loweredCountryCode = countryCode.ToLower();
        if (!Constants.SupportedSubRegionCountryCodeList.Contains(loweredCountryCode)) throw new ArgumentException("Unsupported SubRegion's CountryCode");

        CountryCode = countryCode.ToUpper();
        RegionCode = subRegionCode.ToUpper();
    }
}