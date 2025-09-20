using FlagsRally.Repository;
using FlagsRally.Resources;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FlagsRally.Services;

public class CryptoService
{
    private const int PBKDF2_ITERATIONS = 100_000;
    private const int SALT_SIZE_BYTES = 16; // 128 bits
    private const int KEY_SIZE_BYTES = 32; // 256 bits
    private const int IV_SIZE_BYTES = 12; // 96 bits for GCM
    private const int TAG_SIZE_BYTES = 16; // 128 bits

    // Model for JSON encrypted file format
    private class EncryptedJsonFormat
    {
        public string salt { get; set; } = string.Empty;
        public string iv { get; set; } = string.Empty;
        public string ciphertext { get; set; } = string.Empty;
    }

    public string DecryptJson(byte[] encryptedData)
    {
        var jsonString = Encoding.UTF8.GetString(encryptedData);
        return DecryptJsonFromWebCryptoFormat(jsonString);
    }

    private string DecryptJsonFromWebCryptoFormat(string jsonContent)
    {
        try
        {
            var password = Constants.ENCRYPTION_KEY;
            // Parse the JSON to get salt, iv, and ciphertext
            var encryptedJson = JsonSerializer.Deserialize<EncryptedJsonFormat>(jsonContent);
            if (encryptedJson == null)
            {
                throw new ArgumentException("Invalid JSON format");
            }

            // Base64 decode the components
            var salt = Convert.FromBase64String(encryptedJson.salt);
            var iv = Convert.FromBase64String(encryptedJson.iv);
            var ciphertextWithTag = Convert.FromBase64String(encryptedJson.ciphertext);

            // Validate sizes
            if (salt.Length != SALT_SIZE_BYTES || iv.Length != IV_SIZE_BYTES || ciphertextWithTag.Length < TAG_SIZE_BYTES)
            {
                throw new CryptographicException();
            }

            // Separate the ciphertext and tag
            var ciphertext = new byte[ciphertextWithTag.Length - TAG_SIZE_BYTES];
            var tag = new byte[TAG_SIZE_BYTES];

            Array.Copy(ciphertextWithTag, 0, ciphertext, 0, ciphertext.Length);
            Array.Copy(ciphertextWithTag, ciphertext.Length, tag, 0, TAG_SIZE_BYTES);

            // Derive key using PBKDF2
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, PBKDF2_ITERATIONS, HashAlgorithmName.SHA256);
            var key = pbkdf2.GetBytes(KEY_SIZE_BYTES);

            // Decrypt using AES-GCM
            using var aes = new AesGcm(key, TAG_SIZE_BYTES);
            var plaintext = new byte[ciphertext.Length];


            aes.Decrypt(iv, ciphertext, tag, plaintext);
            return Encoding.UTF8.GetString(plaintext);
        }
        catch (CryptographicException)
        {
            throw new UnauthorizedAccessException(AppResources.InvalidOrCorruptedFile);
        }
    }

    public static bool IsEncryptedFile(string fileName)
    {
        return fileName.EndsWith(".json.encrypted", StringComparison.OrdinalIgnoreCase);
    }
}