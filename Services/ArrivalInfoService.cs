using CountryData.Standard;
using FlagsRally.Models;
using FlagsRally.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlagsRally.Services
{
    public class ArrivalInfoService : IArrivalInfoService
    {
        private readonly IArrivalInfoRepository _arrivalInfoRepository;
        public ArrivalInfoService(IArrivalInfoRepository arrivalInfoRepository) 
        {
            _arrivalInfoRepository = arrivalInfoRepository;
        }

        public async Task<List<Placemark>> GetAllPlacemark()
        {
            var arrivalInfoList = await _arrivalInfoRepository.GetAll();
            return arrivalInfoList.Select(x => JsonSerializer.Deserialize<Placemark>(x.Placemark)).ToList();
        }

        public async Task<List<Placemark>> GetAllPlacemarkByCountryCode(string countryCode)
        {
            var arrivalInfoList = await _arrivalInfoRepository.GetAllByCountryCode(countryCode);
            return arrivalInfoList.Select(x => JsonSerializer.Deserialize<Placemark>(x.Placemark)).ToList();
        }

        public async Task<List<Location>> GetAllLocations()
        {
            var arrivalInfoList = await _arrivalInfoRepository.GetAll();
            return arrivalInfoList.Select(x => JsonSerializer.Deserialize<Placemark>(x.Placemark).Location).ToList();
        }

        public async Task Save(Placemark placemark)
        {
            var arrivalInfo = new ArrivalInfo
            {
                ArrivalDate = DateTime.Now,
                CountryCode = placemark.CountryCode,
                AdminArea = placemark.AdminArea,
                Placemark = JsonSerializer.Serialize(placemark),
            };

            await _arrivalInfoRepository.Insert(arrivalInfo);
        }

        public async Task<List<ArrivalCountry>> GetAllCountries()
        {
            var arrivalInfoList = await _arrivalInfoRepository.GetAll();
            return arrivalInfoList.Select(x => GetArrivalCountry(x)).ToList();
        }

        private ArrivalCountry GetArrivalCountry(ArrivalInfo arrivalInfo)
        {
            var placemark = JsonSerializer.Deserialize<Placemark>(arrivalInfo.Placemark);
            var countryHelper = new CountryHelper();
            return new ArrivalCountry
            {
                Id = arrivalInfo.Id,
                Name = placemark.CountryName,
                ArrivalDate = arrivalInfo.ArrivalDate.ToString(),
#if WINDOWS
                FlagSource = $"https://flagcdn.com/w320/{placemark.CountryCode}.png",
#else
                FlagSource = countryHelper.GetCountryEmojiFlag(arrivalInfo.CountryCode),
#endif
            };
        }
    }
}
