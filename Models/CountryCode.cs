using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FlagsRally.Models
{
    public record struct  CountryCode
    {
        [JsonPropertyName("code")]
        public String Value { get; init; }

        public CountryCode(String value)
        {
            if (value.Length != 2)
            {
                throw new ArgumentException("Country code must be 2 characters long");
            }

            if (!value.All(Char.IsLetter))
            {
                throw new ArgumentException("Country code must be composed of letters only");
            }

            Value = value;
        }
    }
}
