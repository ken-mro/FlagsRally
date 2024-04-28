using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FlagsRally.Models
{
    public record struct Country
    {
        public string Name { get; init; }

        [JsonPropertyName("code")]
        public CountryCode Code { get; init; }

        public Country(string name, CountryCode code)
        {
            Name = name;
            Code = code;
        }

        public static Country GetDefault()
        {
            return new Country("Japan", new CountryCode("JP"));
        }
    }
}
