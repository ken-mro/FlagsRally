using FlagsRally.Models.CustomBoard;
using FlagsRally.Repository;
using System.Text.Json;

namespace FlagsRally.Services;

public class CustomBoardService
{
    private readonly ICustomBoardRepository _customBoardRepository;
    private readonly ICustomLocationDataRepository _customLocationDataRepository;
    private readonly CryptoService _cryptoService;

    public CustomBoardService(ICustomBoardRepository customBoardRepository, ICustomLocationDataRepository customLocationDataRepository, CryptoService cryptoService)
    {
        _customBoardRepository = customBoardRepository;
        _customLocationDataRepository = customLocationDataRepository;
        _cryptoService = cryptoService;
    }

    public CustomBoard GetCustomBoard(CustomBoardJson json)
    {
        return new CustomBoard()
        {
            Name = json.name,
            Url = json.url,
            Width = json.width,
            Height = json.height
        };
    }

    public List<CustomLocation> GetCustomLocations(CustomBoardJson json, CustomBoard customBoard)
    {
        var locations = new List<CustomLocation>();
        foreach (var location in json.locations)
        {
            locations.Add(new CustomLocation
            (
                board: customBoard,
                code: location.code,
                title: location.title,
                subtitle: location.subtitle,
                group: location.group,
                location: new Location()
                {
                    Latitude = location.latitude,
                    Longitude = location.longitude
                },
                arrivalDate: null
            ));
        }
        return locations;
    }

    public async Task<(CustomBoard, IEnumerable<CustomLocationPin>)> SaveBoardAndLocations(Stream stream, string fileName)
    {
        if (stream is null) return new();

        string json = string.Empty;

        // Check if file is encrypted based on extension
        if (CryptoService.IsEncryptedFile(fileName))
        {
            // Read encrypted data
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var encryptedData = memoryStream.ToArray();

            json = _cryptoService.DecryptJson(encryptedData);
        }
        else
        {
            // Read plain text JSON
            using var reader = new StreamReader(stream);
            json = await reader.ReadToEndAsync();
        }

        var customBoardJson = JsonSerializer.Deserialize<CustomBoardJson>(json) ?? new();
        return await SaveBoardAndLocations(customBoardJson);
    }

    private async Task<(CustomBoard,IEnumerable<CustomLocationPin>)> SaveBoardAndLocations(CustomBoardJson json)
    {
        var customBoard = GetCustomBoard(json);
        
        // Check if the board has extension data and use appropriate method
        if (json.ExtensionData != null && json.ExtensionData.Count > 0)
        {
            await _customBoardRepository.InsertOrReplaceWithExtensionDataAsync(customBoard, json.ExtensionData);
        }
        else
        {
            await _customBoardRepository.InsertOrReplaceAsync(customBoard);
        }
        
        var customLocations = GetCustomLocations(json, customBoard);
        // Use the new method that preserves extension data
        var pins = await _customLocationDataRepository.InsertOrReplaceWithExtensionData(customLocations, json.locations);
        return (customBoard, pins);
    }

    // Helper method to serialize extension data
    public static string? SerializeExtensionData(Dictionary<string, JsonElement>? extensionData)
    {
        if (extensionData == null || extensionData.Count == 0)
            return null;
            
        return JsonSerializer.Serialize(extensionData);
    }
    
    // Helper method to deserialize extension data
    public static Dictionary<string, JsonElement>? DeserializeExtensionData(string? extensionData)
    {
        if (string.IsNullOrEmpty(extensionData))
            return null;
            
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(extensionData);
    }
}
