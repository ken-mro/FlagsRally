using FlagsRally.Models.CustomBoard;
using Maui.GoogleMaps;
using SQLite;

namespace FlagsRally.Repository;

public class CustomLocationDataRepository : BaseRepository, ICustomLocationDataRepository
{
    private readonly ICustomBoardRepository _customBoardRepository;
    
    public CustomLocationDataRepository(ICustomBoardRepository customBoardRepository)
    {
        _customBoardRepository = customBoardRepository;
    }

    protected override async Task CreateTableAsync()
    {
        await _conn!.CreateTableAsync<CustomLocationData>();
        // Rows saved before SortIndex existed were inserted in JSON order, so their rowid keeps that order.
        await _conn!.ExecuteAsync("UPDATE CustomLocation SET SortIndex = rowid WHERE SortIndex IS NULL OR SortIndex = 0");
    }

    public async Task<IEnumerable<CustomLocationPin>> GetAllCustomLocationPins()
    {
        await Init();
        var customLocationDataList = await _conn!.Table<CustomLocationData>().ToListAsync();
        // Only places of boards that still exist: with no boards there are no custom pins.
        var boardNames = (await _customBoardRepository.GetAllCustomBoards()).Select(b => b.Name).ToHashSet();
        return customLocationDataList.Where(l => boardNames.Contains(l.BoardName)).Select(GetCustomLocationPin).ToList();
    }

    public async Task<int> DeleteByBoardNameAsync(string boardName)
    {
        await Init();
        return await _conn!.Table<CustomLocationData>().Where(x => x.BoardName == boardName).DeleteAsync();
    }

    public async Task<IEnumerable<CustomLocation>> GetAllCustomLocations()
    {
        await Init();
        var customLocationDataList = await _conn!.Table<CustomLocationData>().OrderBy(x => x.SortIndex).ToListAsync();
        var customBoardList = await _customBoardRepository.GetAllCustomBoards();
        return customLocationDataList.Select(l => GetCustomLocation(customBoardList.Where(b => b.Name == l.BoardName).FirstOrDefault() ?? new(), l)).ToList();
    }

    public async Task<CustomLocation?> GetCustomLocationByCompositeKey(string compositeKey)
    {
        await Init();
        var customLocationData = await _conn!.Table<CustomLocationData>()
                           .Where(x => x.CompositeKey.Equals(compositeKey))
                           .FirstOrDefaultAsync();
        
        if (customLocationData is null)
            return null;

        var customBoardList = await _customBoardRepository.GetAllCustomBoards();
        var customBoard = customBoardList.FirstOrDefault(b => b.Name == customLocationData.BoardName) ?? new();
        
        return GetCustomLocation(customBoard, customLocationData);
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
            ArrivalDate = customLocation.ArrivalDate,
            SortIndex = customLocation.SortIndex
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
        )
        {
            SortIndex = customLocationData.SortIndex
        };
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
