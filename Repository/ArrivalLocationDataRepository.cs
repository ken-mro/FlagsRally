using CountryData.Standard;
using FlagsRally.Models;
using FlagsRally.Resources;
using FlagsRally.Utilities;
using SQLite;
using System.Globalization;
using System.Text.Json;

namespace FlagsRally.Repository;

public class ArrivalLocationRepository : IArrivalLocationDataRepository
{
    string _dbPath = Constants.DataBasePath;
    readonly CountryHelper _countryHelper;

    readonly CustomGeolocation _customGeolocation;

    public string StatusMessage { get; set; }

    private SQLiteAsyncConnection _conn;

    public ArrivalLocationRepository(CountryHelper countryHelper, CustomGeolocation customGeolocation)
    {
        _countryHelper = countryHelper;
        _customGeolocation = customGeolocation;
        _ = Init();
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

    public async Task<List<ArrivalLocation>> GetAllArivalLocations()
    {
        await Init();
        var arrivalLocationDataList = await _conn.Table<ArrivalLocationData>().ToListAsync();
        return arrivalLocationDataList.Select(GetArrivalLocation).ToList();
    }

    public async Task<List<ArrivalLocation>> GetAllByCountryCode(string countryCode)
    {
        await Init();
        var arrivalLocationDataList = await _conn.Table<ArrivalLocationData>().Where(i => i.CountryCode == countryCode).ToListAsync();
        return arrivalLocationDataList.Select(GetArrivalLocation).ToList();
    }

    private ArrivalLocation GetArrivalLocation(ArrivalLocationData ArrivalLocationData)
    {
        if (string.IsNullOrEmpty(ArrivalLocationData.CountryCode))
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
            adminAreaFlagSource = $"https://flagcdn.com/160x120/{subRegionCode?.lower5LetterRegionCode}.png";
            if (ArrivalLocationData?.AdminAreaCode == "DC")
                adminAreaFlagSource = "us_dc.png";

        }
        else if (ArrivalLocationData.CountryCode == "JP")
        {
            var subRegionCode = new SubRegionCode(ArrivalLocationData.CountryCode, ArrivalLocationData.AdminAreaCode);
            adminAreaFlagSource = $"{subRegionCode?.GetImageResourceString()}.png";
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

    public async Task<List<Location>> GetAllLocations()
    {
        await Init(); // Ensure the database connection is initialized
        var arrivalLocationDataList = await _conn.Table<ArrivalLocationData>().ToListAsync();
        return [.. arrivalLocationDataList.Select(GetLocatoin)];

    }

    private Location GetLocatoin(ArrivalLocationData ArrivalLocationData)
    {
        return new Location
        {
            Latitude = ArrivalLocationData.Latitude,
            Longitude = ArrivalLocationData.Longitude
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
            ArrivalDate = ArrivalLocationData.ArrivalDate,
            Name = ArrivalLocationData.AdminAreaName,
            Code = new SubRegionCode
            (
                countryCode: ArrivalLocationData.CountryCode,
                subRegionCode: ArrivalLocationData.AdminAreaCode
            )
        };
    }

    [Obsolete]
    public async Task<bool> UpdateDatabase()
    {
        var databaseInfo = await _conn.GetTableInfoAsync(nameof(ArrivalInfo));
        List<int> deletedIdList = new();

        if (databaseInfo.Any())
        {
            // Get all the data from the ArrivalInfo table
            var arrivalInfoList = await _conn.Table<ArrivalInfo>().ToListAsync();

            // Insert the data into the ArrivalLocation table
            foreach (var arrivalInfo in arrivalInfoList)
            {
                var id = arrivalInfo.Id;
                var placemark = JsonSerializer.Deserialize<Placemark>(arrivalInfo.Placemark);
                var location = placemark.Location;
                string languageCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var arrivalLocationData = await _customGeolocation.GetArrivalLocationAsync(arrivalInfo.ArrivalDate, location, languageCode);

                if (!string.IsNullOrEmpty(arrivalLocationData.CountryCode))
                {
                    var insertedId = await _conn.InsertAsync(arrivalLocationData);
                    if (id > 0) deletedIdList.Add(id);
                }
            }

            foreach(var deletedId in deletedIdList)
            {
                await _conn.Table<ArrivalInfo>().Where(x => x.Id == deletedId).DeleteAsync();
            }

            // Drop the ArrivalInfo table
            await _conn.DropTableAsync<ArrivalInfo>();
        }

        return true;
    }
}
