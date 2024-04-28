using FlagsRally.Models;
using FlagsRally.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlagsRally.Repository
{

    public class CountryRepository
    {
        private IEnumerable<Country> _countryList;

        public CountryRepository(IConfiguration configuration)
        {
            var CountryDtoList = configuration.GetSection("Countries").Get<List<CountryDto>>();
            _countryList = CountryDtoList.Select(x => new Country(x.Name, new CountryCode(x.Code))).ToList();
        }

        public IEnumerable<Country> GetCountries()
        {
            return _countryList;
        }
    }
}
