using FlagsRally.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlagsRally.Services
{
    public interface IArrivalInfoService
    {
        string GetJaSubregionName(SubRegionCode subRegionCode);
    }
}
