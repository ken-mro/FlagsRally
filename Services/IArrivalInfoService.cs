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
        Task Save(Placemark placemark);
        Task<List<Placemark>> GetAllPlacemark();
        Task<List<Placemark>> GetAllPlacemarkByCountryCode(string countryCode);
        Task<List<Location>> GetAllLocations();
        Task<List<ArrivalLocation>> GetAllCountries();
        Task<List<SubRegion>> GetSubRegionsByCountryCode(string countryCode);
    }
}
