using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlagsRally.Models
{
    public class SubRegion
    {
        string Name { get; init; }
        SubRegionCode Code { get; init; }

        public SubRegion(string name, SubRegionCode code)
        {
            Name = name;
            Code = code;
        }
    }
}
