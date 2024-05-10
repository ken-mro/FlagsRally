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
        Task<int> Save(Placemark placemark, DateTime currenTime);
        Task<List<Placemark>> GetAllPlacemark();
        Task<List<Placemark>> GetAllPlacemarkByCountryCode(string countryCode);
        Task<List<Location>> GetAllLocations();
        Task<List<ArrivalLocation>> GetAllCountries();
        Task<List<SubRegion>> GetSubRegionsByCountryCode(string countryCode);
        Task<List<ArrivalLocationPin>> GetArrivalLocationPinsAsync();
    }
}
