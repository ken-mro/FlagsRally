﻿using FlagsRally.Repository;

namespace FlagsRally.Models;

public record SubRegionCode
{
    public string lower5LetterRegionCode { get; init; }
    public string CountryCode { get; init; }
    public SubRegionCode(string fiveLetterRegionCode)
    {
        CountryCode = fiveLetterRegionCode[0..2].ToUpper();
        if (!Constants.SupportedSubRegionCountryCodeList.Contains(CountryCode.ToLower())) throw new ArgumentException("Unsupported SubRegion's CountryCode");
        if (fiveLetterRegionCode.Length != 5) throw new ArgumentException("SubRegionCode must be 5 characters long");
        if (fiveLetterRegionCode[2] != '-') throw new ArgumentException("SubRegionCode must have a hyphen in the middle");

        lower5LetterRegionCode = fiveLetterRegionCode.ToLower();
    }

    public SubRegionCode(string countryCode, string subRegionCode)
    {
        CountryCode = countryCode;
        var loweredCountryCode = countryCode.ToLower();
        var loweredSubRegionCode = subRegionCode.ToLower();
        if (loweredCountryCode.Length != 2 || loweredSubRegionCode.Length != 2) throw new ArgumentException("Unexpected length of argument");
        if (!Constants.SupportedSubRegionCountryCodeList.Contains(loweredCountryCode)) throw new ArgumentException("Unsupported SubRegion's CountryCode");

        lower5LetterRegionCode = loweredCountryCode + "-" + loweredSubRegionCode;
    }

    public string GetImageResourceString()
    {
        return lower5LetterRegionCode.Replace('-', '_');
    }
}