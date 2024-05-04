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
        private List<SubRegion> _subRegionList;

        public ArrivalInfoService(IArrivalInfoRepository arrivalInfoRepository)
        {
            _arrivalInfoRepository = arrivalInfoRepository;

            var jsonFilePath = Path.Combine(FileSystem.AppDataDirectory, "Resources/RegionCode/us.json");

            var names =
                System
                .Reflection
                .Assembly
                .GetExecutingAssembly()
                .GetManifestResourceNames();

            var info = System.Reflection.Assembly.GetExecutingAssembly().GetName();
            using var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream($"{info.Name}.Resources.RegionCode.us.json");
            using var streamReader = new StreamReader(stream, Encoding.UTF8);
            string jsonString = streamReader.ReadToEnd();

            // Deserialize the JSON string into a dictionary
            var subRegionDtolist = JsonSerializer.Deserialize<List<SubRegionDto>>(jsonString);
            _subRegionList = subRegionDtolist.ConvertAll(x => SubRegion.ConvertFrom(x)); ;
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

        public async Task<List<ArrivalLocation>> GetAllCountries()
        {
            var arrivalInfoList = await _arrivalInfoRepository.GetAll();
            return arrivalInfoList.Select(x => GetArrivalCountry(x)).ToList();
        }

        private ArrivalLocation GetArrivalCountry(ArrivalInfo arrivalInfo)
        {
            var placemark = JsonSerializer.Deserialize<Placemark>(arrivalInfo.Placemark);
            var countryHelper = new CountryHelper();

            string countryFlagSource;
#if WINDOWS
            countryFlagSource = $"https://flagcdn.com/w320/{placemark.CountryCode}.png";
#else
            countryFlagSource = countryHelper.GetCountryEmojiFlag(arrivalInfo.CountryCode);
#endif

            string adminAreaFlagSource = "Images/earth_noised.png";
            //string adminAreaFlagSource = countryFlagSource;

            if (placemark.CountryCode == "US")
            {
                var subRegionCode = _subRegionList.Find(x => x.Name == placemark.AdminArea)?.Code.Value.ToLower();
                adminAreaFlagSource = $"https://flagcdn.com/160x120/{subRegionCode}.png";
            }
            else if (placemark.CountryCode == "JP")
            {
                adminAreaFlagSource = $"Images/PrefectureFlags/{arrivalInfo.AdminArea.ToLower()}.png";
            }

            return new ArrivalLocation
            {
                Id = arrivalInfo.Id,
                ArrivalDate = arrivalInfo.ArrivalDate.ToString(),
                CountryCode = placemark.CountryCode,
                CountryName = placemark.CountryName,
                AdminAreaName = placemark.AdminArea,
                CountryFlagSource = countryFlagSource,
                AdminAreaFlagSource = adminAreaFlagSource,
            };
        }
    }
}
