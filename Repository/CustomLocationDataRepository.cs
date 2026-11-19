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
        var boardsByName = (await _customBoardRepository.GetAllCustomBoards()).ToDictionary(b => b.Name);
        return customLocationDataList.Select(l => GetCustomLocation(boardsByName.GetValueOrDefault(l.BoardName) ?? new(), l)).ToList();
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

    /// <summary>
    /// Saves a board's places in one transaction, keeping the check-in of places already saved.
    /// </summary>
    public async Task<IEnumerable<CustomLocationPin>> InsertOrReplace(IEnumerable<CustomLocation> customLocationList)
    {
        await Init();
        var rows = customLocationList.Select(GetCustomLocationData).ToList();

        var boardNames = rows.Select(r => r.BoardName).Distinct().ToList();
        var existingCheckIns = new Dictionary<string, (DateTime? ArrivalDate, bool StampUnseen)>();
        foreach (var boardName in boardNames)
        {
            foreach (var existing in await _conn!.Table<CustomLocationData>().Where(x => x.BoardName == boardName).ToListAsync())
            {
                existingCheckIns[existing.CompositeKey] = (existing.ArrivalDate, existing.StampUnseen);
            }
        }
        foreach (var row in rows)
        {
            if (existingCheckIns.TryGetValue(row.CompositeKey, out var checkIn))
            {
                row.ArrivalDate = checkIn.ArrivalDate;
                row.StampUnseen = checkIn.StampUnseen;
            }
        }

        await _conn!.RunInTransactionAsync(connection =>
        {
            foreach (var row in rows)
            {
                connection.InsertOrReplace(row);
            }
        });
        return rows.Select(GetCustomLocationPin).ToList();
    }

    /// <summary>
    /// Records a check-in (or clears it with null). <paramref name="stampUnseen"/> marks a check-in whose
    /// stamp could not be downloaded; clearing the check-in clears it too.
    /// </summary>
    public async Task<int> UpdateCustomLocation(string key, DateTime? now, bool stampUnseen = false)
    {
        await Init();
        var result = await _conn!.ExecuteAsync("UPDATE CustomLocation SET ArrivalDate = ?, StampUnseen = ? WHERE CompositeKey = ?", now, now is not null && stampUnseen, key);
        return result;
    }

    public async Task<int> MarkStampSeen(string key)
    {
        await Init();
        return await _conn!.ExecuteAsync("UPDATE CustomLocation SET StampUnseen = 0 WHERE CompositeKey = ?", key);
    }

    private CustomLocationPin GetCustomLocationPin(CustomLocationData data)
    {
        return new CustomLocationPin(data);
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
            StampUnseen = customLocation.StampUnseen,
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
            SortIndex = customLocationData.SortIndex,
            StampUnseen = customLocationData.StampUnseen,
        };
    }


}
