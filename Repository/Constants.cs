using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlagsRally.Repository;
public static class Constants
{
    private const string DATABSE_NAME = "FlagsRally.db3";
    private const string GOOGLE_MAP_API_KEY = "PASTE-YOUR-API-KEY-HERE";
    private const string BING_MAP_API_KEY = "PASTE-YOUR-API-KEY-HERE";
    private const string REVENUECAT_API_KEY_ANDROID = "PASTE-YOUR-API-KEY-HERE";
    private const string REVENUECAT_API_KEY_IOS = "PASTE-YOUR-API-KEY-HERE";
    public static string DatabaseName => DATABSE_NAME;
    public static string DataBasePath => Path.Combine(FileSystem.AppDataDirectory, DATABSE_NAME);
    public static string GoogleMapApiKey => GOOGLE_MAP_API_KEY;
    public static string BingMapApiKey => BING_MAP_API_KEY;
    public static string RevenueCatApiKeyAndroid => REVENUECAT_API_KEY_ANDROID;
    public static string RevenueCatApiKeyIos => REVENUECAT_API_KEY_IOS;
    public static List<string> SupportedSubRegionCountryCodeList => new List<string> { "ca", "fr", "de", "it", "jp", "my", "nl", "no", "es", "ch", "us" };
}