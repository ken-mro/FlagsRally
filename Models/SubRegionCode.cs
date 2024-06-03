﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FlagsRally.Models
{
    public record SubRegionCode
    {
        public string lower5LetterRegionCode { get; init; }
        public SubRegionCode(string fiveLetterRegionCode)
        {
            if (fiveLetterRegionCode[0..2] != "JP" && fiveLetterRegionCode[0..2] != "US" && fiveLetterRegionCode[0..2] != "DE") throw new ArgumentException("Unexpected country's SubRegionCode");

            if (fiveLetterRegionCode.Length != 5) throw new ArgumentException("SubRegionCode must be 5 characters long");

            if (fiveLetterRegionCode[2] != '-') throw new ArgumentException("SubRegionCode must have a hyphen in the middle");

            if (fiveLetterRegionCode[0..2] == "JP" && !int.TryParse(fiveLetterRegionCode[3..5], out _)) throw new ArgumentException("JP's SubRegionCode has 2 digits");

            if (fiveLetterRegionCode[0..2] == "US" && !fiveLetterRegionCode[3..5].All(Char.IsLetter)) throw new ArgumentException("US's SubRegionCode only has 2 letters");

            if (fiveLetterRegionCode[0..2] == "DE" && !fiveLetterRegionCode[3..5].All(Char.IsLetter)) throw new ArgumentException("DE's SubRegionCode only has 2 letters");

            lower5LetterRegionCode = fiveLetterRegionCode.ToLower();
        }

        public SubRegionCode(string countryCode, string subRegionCode)
        {
            var loweredCountryCode = countryCode.ToLower();
            var loweredSubRegionCode = subRegionCode.ToLower();

            if (loweredCountryCode.Length != 2 || loweredSubRegionCode.Length != 2) throw new ArgumentException("Unexpected length of argument");

            if (loweredCountryCode[0..2] != "jp" && loweredCountryCode[0..2] != "us" && loweredCountryCode[0..2] != "de") throw new ArgumentException("Unexpected Country Code");


            if (loweredCountryCode[0..2] == "jp" && !int.TryParse(subRegionCode[0..2], out _)) throw new ArgumentException("JP's SubRegionCode has 2 digits");

            if (loweredCountryCode[0..2] == "us" && !loweredSubRegionCode[0..2].All(Char.IsLetter)) throw new ArgumentException("US's SubRegionCode only has 2 letters");

            if (loweredCountryCode[0..2] == "de" && !loweredSubRegionCode[0..2].All(Char.IsLetter)) throw new ArgumentException("US's SubRegionCode only has 2 letters");

            lower5LetterRegionCode = loweredCountryCode + "-" + loweredSubRegionCode;
        }

        public string GetImageResourceString()
        {
            return lower5LetterRegionCode.Replace('-', '_');
        }
    }
}