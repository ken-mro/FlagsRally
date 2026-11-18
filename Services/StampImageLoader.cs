namespace FlagsRally.Services;

/// <summary>
/// Downloads a board's stamp image before a check-in is recorded, so the stamp that is pressed down
/// (with confetti) is never an empty frame. A slow, missing or broken image gives null, and the
/// check-in is left for later.
/// </summary>
public class StampImageLoader
{
    /// <summary>How long a check-in waits for its stamp before asking to try again later.</summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    /// <summary>Stamps are small pictures; anything larger is not one.</summary>
    public const int MaxBytes = 5 * 1024 * 1024;

    readonly HttpClient _http;

    public StampImageLoader() : this(new HttpClientHandler()) { }

    public StampImageLoader(HttpMessageHandler handler)
    {
        _http = new HttpClient(handler)
        {
            Timeout = Timeout,
            MaxResponseContentBufferSize = MaxBytes,
        };
    }

    /// <summary>
    /// The image's bytes, or null when it cannot be had in time: no connection, a timeout, an error
    /// status, or an answer that is not a picture the app can show (an HTML error page, say).
    /// </summary>
    public async Task<byte[]?> LoadAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            return null;
        try
        {
            using var response = await _http.GetAsync(uri, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return IsPicture(bytes) ? bytes : null;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // No connection, a timeout, or more than MaxBytes
            return null;
        }
    }

    /// <summary>
    /// Whether the bytes start like a PNG, JPEG, GIF, WebP or BMP, the formats the image view decodes
    /// on both platforms. Servers do not always send a useful content type, so the bytes decide.
    /// </summary>
    public static bool IsPicture(ReadOnlySpan<byte> bytes) =>
        bytes.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])   // PNG
        || bytes.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF])                             // JPEG
        || bytes.StartsWith("GIF8"u8)
        || (bytes.Length >= 12 && bytes.StartsWith("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8))
        || (bytes.Length >= 26 && bytes.StartsWith("BM"u8));
}
