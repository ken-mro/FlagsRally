using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlagsRally.Repository;
public static class Constants
{
    private const string DATABSE_NAME = "FlagsRally.db3";
    public const string GOOGLE_MAP_API_KEY = "PASTE-YOUR-API-KEY-HERE";

    public const string REVENUECAT_API_KEY_ANDROID = "PASTE-YOUR-API-KEY-HERE";
    public const string REVENUECAT_API_KEY_IOS = "PASTE-YOUR-API-KEY-HERE";
    public const string TOMTOM_API_KEY = "PASTE-YOUR-API-KEY-HERE";

    public const string SYNCFUSIOHN_LICENSE_KEY = "PASTE-YOUR-LISENCE-KEY";

    public const string GEOJSON_RESOURCE_BASE_URL = "PASTE_YOUR_BASE_URL";
    public static string DatabaseName => DATABSE_NAME;
    public static string DataBasePath => Path.Combine(FileSystem.AppDataDirectory, DATABSE_NAME);
    public static List<string> SupportedSubRegionCountryCodeList => new List<string> { "at", "ca", "cz", "fr", "de", "it", "jp", "my", "nl", "no", "pt", "es", "ch", "ua", "us" };
}