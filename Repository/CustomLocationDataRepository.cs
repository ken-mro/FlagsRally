using FlagsRally.Models.CustomBoard;
using Maui.GoogleMaps;
using SQLite;

namespace FlagsRally.Repository;

public class CustomLocationDataRepository : ICustomLocationDataRepository
{
    private string _dbPath = Constants.DataBasePath;
    private SQLiteAsyncConnection? _conn;

    private readonly ICustomBoardRepository _customBoardRepository;
    public CustomLocationDataRepository(ICustomBoardRepository customBoardRepository)
    {
        _customBoardRepository = customBoardRepository;
    }

    private async Task Init()
    {
        if (_conn != null)
            return;

        _conn = new SQLiteAsyncConnection(_dbPath);
        await _conn.CreateTableAsync<CustomLocationData>();
    }

    public async Task<IEnumerable<CustomLocationPin>> GetAllCustomLocationPins()
    {
        await Init();
        var customLocationDataList = await _conn!.Table<CustomLocationData>().ToListAsync();
        return customLocationDataList.Select(GetCustomLocationPin).ToList();
    }

    public async Task<IEnumerable<CustomLocation>> GetAllCustomLocations()
    {
        await Init();
        var customLocationDataList = await _conn!.Table<CustomLocationData>().ToListAsync();
        var customBoardList = await _customBoardRepository.GetAllCustomBoards();
        return customLocationDataList.Select(l => GetCustomLocation(customBoardList.Where(b => b.Name == l.BoardName).FirstOrDefault() ?? new(), l)).ToList();
    }

    public async Task<IEnumerable<CustomLocationPin>> InsertOrReplace(IEnumerable<CustomLocation> customLocationList)
    {
        await Init();
        var resultLocationList = new List<CustomLocationPin>();
        foreach (var customLocation in customLocationList)
        {
            await InsertOrReplace(customLocation);
            var customLoationData = GetCustomLocationData(customLocation);
            resultLocationList.Add(GetCustomLocationPin(customLoationData));
        }
        return resultLocationList;
    }

    public async Task<int> UpdateCustomLocation(string key, DateTime? now)
    {
        await Init();
        var result = await _conn!.ExecuteAsync("UPDATE CustomLocation SET ArrivalDate = ? WHERE CompositeKey = ?", now, key);
        return result;
    }

    private CustomLocationPin GetCustomLocationPin(CustomLocationData data)
    {
        return new CustomLocationPin(data);
    }

    private async Task<CustomLocationData> GetCustomLocation(string compositeKey)
    {
        await Init();
        return await _conn!.Table<CustomLocationData>()
                           .Where(x => x.CompositeKey.Equals(compositeKey))
                           .FirstOrDefaultAsync();
    }

    private CustomLocationData GetCustomLocationData(CustomLocation customLocation)
    {
        return new CustomLocationData
        {
            CompositeKey = customLocation.CompositeKey,
            BoardName = customLocation.Board.Name,
            Code = customLocation.Code,
            Title = customLocation.Title,
            Subtitle = customLocation.Subtitle,
            Group = customLocation.Group,
            Latitude = customLocation.Location.Latitude,
            Longitude = customLocation.Location.Longitude,
            ArrivalDate = customLocation.ArrivalDate
        };
    }

    private CustomLocation GetCustomLocation(CustomBoard customBoard, CustomLocationData customLocationData)
    {
        return new CustomLocation
        (
            board: customBoard,
            code: customLocationData.Code,
            title: customLocationData.Title,
            subtitle: customLocationData.Subtitle,
            group: customLocationData.Group,
            location: new Location
            {
                Latitude = customLocationData.Latitude,
                Longitude = customLocationData.Longitude
            },
            arrivalDate: customLocationData.ArrivalDate
        );
    }

    private async Task<int> InsertOrReplace(CustomLocation customLocation)
    {
        await Init();
        var customLocationData = GetCustomLocationData(customLocation);
        var existingCustomLocationData = await GetCustomLocation(customLocation.CompositeKey);
        if (existingCustomLocationData is not null)
        {
            customLocationData.ArrivalDate = existingCustomLocationData.ArrivalDate;
            return await _conn!.InsertOrReplaceAsync(customLocationData);
        }
        
        return await _conn!.InsertOrReplaceAsync(customLocationData);
    }
}
