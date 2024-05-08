using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlagsRally.Models
{
    public class SubRegionData
    {
        //For example, "Hokkaido" for Hokkaido, "California" for California
        public string Name { get; init; }

        //For example, "JP-01" for Hokkaido, "US-CA" for California
        public string Code { get; init; }

        public SubRegionData (string name, string code)
        {
            if (code[0..2] != "JP" && code[0..2] != "US") throw new ArgumentException("Unexpected country's SubRegionCode");

            if (code.Length != 5) throw new ArgumentException("SubRegionCode must be 5 characters long");

            if (code[2] != '-') throw new ArgumentException("SubRegionCode must have a hyphen in the middle");

            if (code[0..2] == "JP" && !int.TryParse(code[3..5], out _)) throw new ArgumentException("JP's SubRegionCode has 2 digits");

            if (code[0..2] == "US" && !code[3..5].All(Char.IsLetter)) throw new ArgumentException("US's SubRegionCode only has 2 letters");

            Code = code;
            Name = name;
        }
    }
}
