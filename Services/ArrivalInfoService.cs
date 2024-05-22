using CountryData.Standard;
using FlagsRally.Models;
using FlagsRally.Repository;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlagsRally.Services
{
    public class ArrivalInfoService : IArrivalInfoService
    {
        private readonly IArrivalInfoRepository _arrivalInfoRepository;
        private Dictionary<string, Dictionary<string, SubRegionCode>> _subRegionCodeMap = new();

        public ArrivalInfoService(IArrivalInfoRepository arrivalInfoRepository)
        {
            _arrivalInfoRepository = arrivalInfoRepository;
            var countryHelper = new CountryHelper();

            var info = System.Reflection.Assembly.GetExecutingAssembly().GetName();
            using var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream($"{info.Name}.Resources.RegionCode.jp.json");
            using var streamReader = new StreamReader(stream!, Encoding.UTF8);
            string jsonString = streamReader.ReadToEnd();
            var jaJpSubRegionDataList = JsonSerializer.Deserialize<List<SubRegionData>>(jsonString);
            var enJpSubRegionDataList = countryHelper.GetCountryByCode("JP").Regions.ConvertAll(x => new SubRegionData(x.Name, "JP-" + x.ShortCode));
            jaJpSubRegionDataList?.AddRange(enJpSubRegionDataList);

            var jpSubRegionDict = jaJpSubRegionDataList?.ToDictionary(x => x.Name, x => new SubRegionCode(x.Code));
            var usSubRegionDict = countryHelper.GetCountryByCode("US").Regions
                .Where(x => !new[] { "AA", "AE", "AP", "AS", "DC", "FM", "GU", "MH", "MP", "PR", "PW", "VI" }.Contains(x.ShortCode)).ToList()
                .ToDictionary(x => x.Name, x => new SubRegionCode("US", x.ShortCode));

            _subRegionCodeMap.Add("JP", jpSubRegionDict!);
            _subRegionCodeMap.Add("US", usSubRegionDict);
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

        public async Task<int> Save(Placemark placemark, DateTime currenTime)
        {
            var arrivalInfo = new ArrivalInfo
            {
                ArrivalDate = currenTime,
                CountryCode = placemark.CountryCode,
                AdminArea = placemark.AdminArea,
                Placemark = JsonSerializer.Serialize(placemark),
            };

            return await _arrivalInfoRepository.Insert(arrivalInfo);
        }

        public async Task<List<ArrivalLocation>> GetAllCountries()
        {
            var arrivalInfoList = await _arrivalInfoRepository.GetAll();
            return arrivalInfoList.Select(GetArrivalCountry).ToList();
        }

        private ArrivalLocation GetArrivalCountry(ArrivalInfo arrivalInfo)
        {
            var placemark = JsonSerializer.Deserialize<Placemark>(arrivalInfo.Placemark);
            var countryHelper = new CountryHelper();

            string countryFlagSource;

#if WINDOWS
            countryFlagSource = $"https://flagcdn.com/160x120/{placemark.CountryCode.ToLower()}.png";
#else
            countryFlagSource =  countryHelper.GetCountryEmojiFlag(arrivalInfo.CountryCode);
#endif

            string adminAreaFlagSource = "earth.png";
            _subRegionCodeMap.TryGetValue(placemark!.CountryCode, out var usSubRegionDict);
            
            if (placemark.CountryCode == "US")
            {
                usSubRegionDict!.TryGetValue(placemark.AdminArea, out var subRegionCode);
                adminAreaFlagSource = $"https://flagcdn.com/160x120/{subRegionCode?.lower5LetterRegionCode}.png";

            }
            else if (placemark.CountryCode == "JP")
            {
                usSubRegionDict!.TryGetValue(placemark.AdminArea, out var subRegionCode);
                adminAreaFlagSource = $"{subRegionCode?.GetImageResourceString()}.png";
            }

            return new ArrivalLocation
            {
                Id = arrivalInfo.Id,
                ArrivalDate = arrivalInfo.ArrivalDate,
                CountryCode = placemark.CountryCode,
                CountryName = placemark.CountryName,
                AdminAreaName = placemark.AdminArea,
                CountryFlagSource = countryFlagSource,
                AdminAreaFlagSource = adminAreaFlagSource,
            };
        }

        public async Task<List<SubRegion>> GetSubRegionsByCountryCode(string countryCode)
        {
            var arrivalInfoList = await _arrivalInfoRepository.GetAllByCountryCode(countryCode);
             return arrivalInfoList.ConvertAll(GetSubRegion).Where(x => x.Code is not null).ToList();
        }

        private SubRegion GetSubRegion(ArrivalInfo arrivalInfo)
        {
            var placemark = JsonSerializer.Deserialize<Placemark>(arrivalInfo.Placemark);
            var countryHelper = new CountryHelper();

            var result = _subRegionCodeMap.TryGetValue(placemark!.CountryCode, out var usSubRegionDict);
            if (!result) throw new ArgumentException("Unexpected country's SubRegionCode");

            usSubRegionDict!.TryGetValue(placemark.AdminArea, out SubRegionCode subRegionCode);

            return new SubRegion
            {
                ArrivalDate = arrivalInfo.ArrivalDate,
                Name = placemark.AdminArea,
                Code = subRegionCode!,
            };
        }

        public async Task<List<ArrivalLocationPin>> GetArrivalLocationPinsAsync()
        {
            var arrivalInfoList = await _arrivalInfoRepository.GetAll();
            return arrivalInfoList.Select(GetArrivalLocationPin).ToList();
        }

        private ArrivalLocationPin GetArrivalLocationPin(ArrivalInfo arrivalInfo)
        {
            var placemark = JsonSerializer.Deserialize<Placemark>(arrivalInfo.Placemark);
            return new ArrivalLocationPin
            {
                Id = arrivalInfo.Id,
                ArrivalDate = arrivalInfo.ArrivalDate,
                PinLocation = new Location
                {
                    Latitude = placemark.Location.Latitude,
                    Longitude = placemark.Location.Longitude,
                }
            };
        }

        public string GetJaSubregionName(SubRegionCode subRegionCode)
        {
            var key = _subRegionCodeMap["JP"].FirstOrDefault(x => x.Value == subRegionCode).Key;
            return key;
        }
    }
}
