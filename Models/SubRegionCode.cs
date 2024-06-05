using FlagsRally.Repository;

namespace FlagsRally.Models;

public record SubRegionCode
{
    public string lowerCountryCodeHyphenSubRegionCode { get; init; }
    public string CountryCode { get; init; }
    public SubRegionCode(string countryCodeHyphenSubRegionCode)
    {
        CountryCode = countryCodeHyphenSubRegionCode[0..2].ToUpper();
        if (!Constants.SupportedSubRegionCountryCodeList.Contains(CountryCode.ToLower())) throw new ArgumentException("Unsupported SubRegion's CountryCode");
        if (countryCodeHyphenSubRegionCode[2] != '-') throw new ArgumentException("SubRegionCode must have a hyphen in the middle");

        lowerCountryCodeHyphenSubRegionCode = countryCodeHyphenSubRegionCode.ToLower();
    }

    public SubRegionCode(string countryCode, string subRegionCode)
    {
        CountryCode = countryCode;
        var loweredCountryCode = countryCode.ToLower();
        var loweredSubRegionCode = subRegionCode.ToLower();
        if (!Constants.SupportedSubRegionCountryCodeList.Contains(loweredCountryCode)) throw new ArgumentException("Unsupported SubRegion's CountryCode");

        lowerCountryCodeHyphenSubRegionCode = loweredCountryCode + "-" + loweredSubRegionCode;
    }

    public string GetImageResourceString()
    {
        return lowerCountryCodeHyphenSubRegionCode.Replace('-', '_');
    }
}