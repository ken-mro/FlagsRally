using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FlagsRally.Repository;
public static class Constants
{
    private const string DATABASE_NAME = "FlagsRally.db3";
    private const string DATABASE_PASSWORD = "YOUR-DATABASE-PASSWORD";
    private const string BACKUP_FILE_PATTERN = @"^backup_flagsrally_\d{14}\.zip$";

    public const string ENCRYPTION_KEY = "ENCRYPTION_KEY";

    public const string GOOGLE_MAP_API_KEY = "PASTE-YOUR-API-KEY-HERE";

    public const string REVENUECAT_API_KEY_ANDROID = "PASTE-YOUR-API-KEY-HERE";
    public const string REVENUECAT_API_KEY_IOS = "PASTE-YOUR-API-KEY-HERE";
    public const string TOMTOM_API_KEY = "PASTE-YOUR-API-KEY-HERE";

    public const string SYNCFUSIOHN_LICENSE_KEY = "PASTE-YOUR-LISENCE-KEY";

    public const string GEOJSON_RESOURCE_BASE_URL = "PASTE_YOUR_BASE_URL";
    public static string DatabaseName => DATABASE_NAME;
    public static string DatabasePassword => DATABASE_PASSWORD;
    public static string BackupZipName => $"backup_flagsrally_{DateTime.Now:yyyyMMddHHmmss}.zip";
    public static string DataBasePath => Path.Combine(FileSystem.AppDataDirectory, DATABASE_NAME);
    public static List<string> SupportedSubRegionCountryCodeList => new List<string> { "at", "ca", "cz", "fr", "de", "it", "jp", "my", "nl", "no", "pt", "es", "ch", "ua", "us" };
    
    /// <summary>
    /// Validates if the given filename matches the backup file pattern (backup_flagsrally_YYYYMMDDHHMMSS.zip)
    /// </summary>
    public static bool IsValidBackupFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;
            
        return Regex.IsMatch(fileName, BACKUP_FILE_PATTERN);
    }
}