using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FlagsRally.Models
{
    public class SubRegionCode
    {
        public string Value { get; init; }
        public SubRegionCode(string value)
        {
            if (value[0..2] != "JP" && value[0..2] != "US") throw new ArgumentException("Unexpected country's SubRegionCode");

            if (value.Length != 5) throw new ArgumentException("SubRegionCode must be 5 characters long");

            if (value[2] != '-') throw new ArgumentException("SubRegionCode must have a hyphen in the middle");

            if (value[0..2] == "JP" && !int.TryParse(value[3..5], out _)) throw new ArgumentException("JP's SubRegionCode has 2 digits");

            if (value[0..2] == "US" && !value[3..5].All(Char.IsLetter)) throw new ArgumentException("US's SubRegionCode only has 2 letters");

            Value = value;
        }
    }
}
