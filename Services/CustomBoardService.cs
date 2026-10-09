using FlagsRally.Models.CustomBoard;
using FlagsRally.Repository;
using FlagsRally.Resources;
using System.Text;
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

    /// <summary>
    /// Deletes the boards together with their locations (including check-in records).
    /// </summary>
    public async Task DeleteBoardsAsync(IEnumerable<string> boardNames)
    {
        foreach (var boardName in boardNames)
        {
            await _customLocationDataRepository.DeleteByBoardNameAsync(boardName);
            await _customBoardRepository.DeleteAsync(boardName);
        }
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
        foreach (var (location, index) in json.locations.Select((x, i) => (x, i)))
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
            )
            {
                SortIndex = index + 1
            });
        }
        return locations;
    }

    public async Task<(CustomBoard, IEnumerable<CustomLocationPin>)> SaveBoardAndLocations(Stream stream, string fileName)
    {
        if (stream is null) return new();

        var customBoardJson = await ReadBoardJsonAsync(stream, fileName);
        return await SaveBoardAndLocations(customBoardJson);
    }

    /// <summary>
    /// Reads a board file (.json, or .json.encrypted which is decrypted first) without saving it.
    /// </summary>
    public async Task<CustomBoardJson> ReadBoardJsonAsync(Stream stream, string fileName)
    {
        var data = await ReadLimitedAsync(stream, CustomBoardFile.MaxBytes);

        // Check if file is encrypted based on extension
        var json = CryptoService.IsEncryptedFile(fileName)
            ? _cryptoService.DecryptJson(data)
            : Encoding.UTF8.GetString(data);

        CustomBoardJson? board;
        try
        {
            board = JsonSerializer.Deserialize<CustomBoardJson>(json);
        }
        catch (JsonException)
        {
            board = null;
        }

        if (board is null || !IsValid(board))
        {
            throw new InvalidOperationException(AppResources.InvalidOrCorruptedFile);
        }
        board.width = Math.Clamp(board.width, 0, MaxTileSize);
        board.height = Math.Clamp(board.height, 0, MaxTileSize);
        return board;
    }

    const int MaxTileSize = 4096;
    const int MaxLocations = 10_000;

    /// <summary>
    /// Boards are shared between people, so only accept what the app can show safely: a name, an
    /// http(s) image template (or none), and places with a unique, non-empty code.
    /// </summary>
    public static bool IsValid(CustomBoardJson board)
    {
        if (string.IsNullOrWhiteSpace(board.name)) return false;
        if (board.locations is null || board.locations.Length == 0 || board.locations.Length > MaxLocations) return false;
        if (board.locations.Any(l => l is null || string.IsNullOrWhiteSpace(l.code))) return false;
        if (board.locations.Select(l => l.code).Distinct().Count() != board.locations.Length) return false;

        if (string.IsNullOrEmpty(board.url)) return true;
        return Uri.TryCreate(board.url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }

    // Reads at most maxBytes, so an oversized file fails fast instead of filling memory.
    static async Task<byte[]> ReadLimitedAsync(Stream stream, int maxBytes)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk)) > 0)
        {
            if (buffer.Length + read > maxBytes)
            {
                throw new InvalidOperationException(AppResources.InvalidOrCorruptedFile);
            }
            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
    }

    public async Task<(CustomBoard,IEnumerable<CustomLocationPin>)> SaveBoardAndLocations(CustomBoardJson json)
    {
        var customBoard = GetCustomBoard(json);
        await _customBoardRepository.InsertOrReplaceAsync(customBoard);
        var customLocations = GetCustomLocations(json, customBoard);
        var pins = await _customLocationDataRepository.InsertOrReplace(customLocations);
        return (customBoard, pins);
    }
}
