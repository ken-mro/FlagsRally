using FlagsRally.Models;
using FlagsRally.Resources;
using FlagsRally.Utilities;
using SQLite;

namespace FlagsRally.Repository;

public class ArrivalLocationDataRepository : IArrivalLocationDataRepository
{
    private string _dbPath = Constants.DataBasePath;
    private readonly CustomCountryHelper _countryHelper;
    private readonly CustomGeolocation _customGeolocation;
    private SQLiteAsyncConnection _conn;

    public ArrivalLocationDataRepository(CustomCountryHelper countryHelper, CustomGeolocation customGeolocation)
    {
        _countryHelper = countryHelper;
        _customGeolocation = customGeolocation;
    }

    private async Task Init()
    {
        if (_conn != null)
            return;

        _conn = new SQLiteAsyncConnection(_dbPath);
        await _conn.CreateTableAsync<ArrivalLocationData>();
    }

    public async Task<int> DeleteAsync(int Id)
    {
        await Init();
        return await _conn.Table<ArrivalLocationData>().Where(i => i.Id == Id).DeleteAsync();
    }

    public async Task<List<ArrivalLocation>> GetAllArrivalLocations()
    {
        await Init();
        var arrivalLocationDataList = await _conn.Table<ArrivalLocationData>().ToListAsync();
        return arrivalLocationDataList.Select(GetArrivalLocation).ToList();
    }

    private ArrivalLocation GetArrivalLocation(ArrivalLocationData ArrivalLocationData)
    {
        if (string.IsNullOrEmpty(ArrivalLocationData.CountryName)
            && string.IsNullOrEmpty(ArrivalLocationData.AdminAreaName)
            && string.IsNullOrEmpty(ArrivalLocationData.LocalityName))
        {
            return new ArrivalLocation()
            {
                Id = ArrivalLocationData.Id,
                ArrivalDate = ArrivalLocationData.ArrivalDate,
                CountryCode = ArrivalLocationData.CountryCode,
                CountryName = AppResources.UnexploredLocation,
                CountryFlagSource = "🏝️",
                AdminAreaName = AppResources.UnexploredLocation,
                AdminAreaCode = ArrivalLocationData.AdminAreaCode,
                AdminAreaFlagSource = "unknown_arrival.png",
                LocalityName = AppResources.UnexploredLocation,
                Location = new Location
                {
                    Latitude = ArrivalLocationData.Latitude,
                    Longitude = ArrivalLocationData.Longitude
                },
            };
        }

        if (string.IsNullOrEmpty(ArrivalLocationData.CountryName))
        {
            return new ArrivalLocation()
            {
                Id = ArrivalLocationData.Id,
                ArrivalDate = ArrivalLocationData.ArrivalDate,
                CountryCode = ArrivalLocationData.CountryCode,
                CountryName = ArrivalLocationData.CountryName,
                CountryFlagSource = "🏖️",
                AdminAreaName = ArrivalLocationData.AdminAreaName,
                AdminAreaCode = ArrivalLocationData.AdminAreaCode,
                AdminAreaFlagSource = "earth_noised.png",
                LocalityName = ArrivalLocationData.LocalityName,
                Location = new Location
                {
                    Latitude = ArrivalLocationData.Latitude,
                    Longitude = ArrivalLocationData.Longitude
                },
            };
        }


        string countryFlagSource;

#if WINDOWS
        countryFlagSource = $"https://flagcdn.com/160x120/{ArrivalLocationData.CountryCode.ToLower()}.png";
#else
        countryFlagSource = _countryHelper.GetCountryEmojiFlag(ArrivalLocationData.CountryCode);
#endif

        string adminAreaFlagSource = string.Empty;

        if (ArrivalLocationData.CountryCode == "US")
        {
            var subRegionCode = new SubRegionCode(ArrivalLocationData.CountryCode, ArrivalLocationData.AdminAreaCode);
            adminAreaFlagSource = $"https://flagcdn.com/160x120/{subRegionCode?.lowerCountryCodeHyphenRegionCode}.png";
            if (ArrivalLocationData?.AdminAreaCode == "DC")
                adminAreaFlagSource = "us_dc.png";
        }
        else if (Constants.SupportedSubRegionCountryCodeList.Contains(ArrivalLocationData.CountryCode.ToLower()))
        {
            if (!string.IsNullOrEmpty(ArrivalLocationData.AdminAreaCode))
            {
                var subRegionCode = new SubRegionCode(ArrivalLocationData.CountryCode, ArrivalLocationData.AdminAreaCode);
                adminAreaFlagSource = $"{subRegionCode?.ImageResourceString}.png";
            }
        }

        return new ArrivalLocation
        {
            Id = ArrivalLocationData.Id,
            ArrivalDate = ArrivalLocationData.ArrivalDate,
            CountryCode = ArrivalLocationData.CountryCode,
            CountryName = ArrivalLocationData.CountryName,
            CountryFlagSource = countryFlagSource,
            AdminAreaName = ArrivalLocationData.AdminAreaName,
            AdminAreaCode = ArrivalLocationData.AdminAreaCode,
            AdminAreaFlagSource = adminAreaFlagSource,
            LocalityName = ArrivalLocationData.LocalityName,
            Location = new Location
            {
                Latitude = ArrivalLocationData.Latitude,
                Longitude = ArrivalLocationData.Longitude
            },
        };
    }

    public async Task<int> Save(ArrivalLocationData arrivalLocationData)
    {
        await Init();
        return await _conn.InsertAsync(arrivalLocationData);
    }

    public async Task<List<ArrivalLocationPin>> GetArrivalLocationPinsAsync()
    {
        await Init();
        var arrivalLocationList = await _conn.Table<ArrivalLocationData>().ToListAsync();
        return [.. arrivalLocationList.Select(GetArrivalLocationPin)];
    }

    private ArrivalLocationPin GetArrivalLocationPin(ArrivalLocationData ArrivalLocationData)
    {
        return new ArrivalLocationPin
        {
            Id = ArrivalLocationData.Id,
            ArrivalDate = ArrivalLocationData.ArrivalDate,
            PinLocation = new Location
            {
                Latitude = ArrivalLocationData.Latitude,
                Longitude = ArrivalLocationData.Longitude,
            }
        };
    }

    public async Task<List<SubRegion>> GetSubRegionsByCountryCode(string countryCode)
    {
        await Init();
        var arrivalLocationList = await _conn.Table<ArrivalLocationData>().Where(i => i.CountryCode == countryCode).ToListAsync();
        return [.. arrivalLocationList.Select(GetArrivalLocationByLocation)];
    }

    private SubRegion GetArrivalLocationByLocation(ArrivalLocationData ArrivalLocationData)
    {
        return new SubRegion
        {
            Id = ArrivalLocationData.Id,
            ArrivalDate = ArrivalLocationData.ArrivalDate,
            Name = ArrivalLocationData.AdminAreaName,
            EnAdminAreaName = ArrivalLocationData.EnAdminAreaName,
            Code = new SubRegionCode
            (
                countryCode: ArrivalLocationData.CountryCode,
                subRegionCode: ArrivalLocationData.AdminAreaCode
            )
        };
    }

    public async Task<int> UpdateAdminAreaCode(int Id, string adminAreaCode)
    {
        if (Id == 0) return 0;
        await Init();
        var result = await _conn.ExecuteAsync("UPDATE ArrivalLocation SET AdminAreaCode = ? WHERE Id = ?", adminAreaCode, Id);
        return result;
    }
}
