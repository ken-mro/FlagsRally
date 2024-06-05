using CountryData.Standard;
using FlagsRally.Models;
using FlagsRally.Repository;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace FlagsRally.Utilities;

public class CustomGeolocation
{
    private readonly CustomCountryHelper _countryHelper;

    public CustomGeolocation(CustomCountryHelper countryHelper)
    {
        _countryHelper = countryHelper;
    }

    public async Task<ArrivalLocationData> GetArrivalLocationAsync(DateTime datetime, Location location, string languageCode)
    {
        var jsonObject = await GetRequestForLocationInfo(location, languageCode);
        var result = jsonObject["results"]?.Value<JArray>()?.FirstOrDefault();
        var addressComponent = result?["address_components"];
        var country = addressComponent?.Where(ac => ac["types"].Values<string>()
                                    .Contains("country")).FirstOrDefault();
        JObject? enJsonObject;
        if (country is null)
        {
            jsonObject = await GetAllRequestsForLocationInfo(location, languageCode);
            enJsonObject = languageCode == "en" ? jsonObject : await GetAllRequestsForLocationInfo(location, "en");
        }
        else
        {
            enJsonObject = languageCode == "en" ? jsonObject : await GetRequestForLocationInfo(location, "en");
        }
        
        return GenerateFrom(datetime, jsonObject, enJsonObject, location);
    }

    private ArrivalLocationData GenerateFrom(DateTime datetime, JObject jsonObject, JObject enJsonObject, Location location)
    {
        var result = jsonObject["results"]?.Value<JArray>()?.FirstOrDefault();
        var addressComponent = result?["address_components"];
        var country = addressComponent?.Where(ac => ac["types"].Values<string>()
                                    .Contains("country")).FirstOrDefault();
        var adminArea = addressComponent?.Where(ac => ac["types"].Values<string>()
                                    .Contains("administrative_area_level_1")).FirstOrDefault();
        var locality = addressComponent?.Where(ac => ac["types"].Values<string>()
                                    .Contains("locality")).FirstOrDefault();

        var enResult = enJsonObject["results"]?.Value<JArray>()?.FirstOrDefault();
        var enAddressComponent = enResult?["address_components"];
        var enCountry = enAddressComponent?.Where(ac => ac["types"].Values<string>()
                                    .Contains("country")).FirstOrDefault();
        var enAdminArea = enAddressComponent?.Where(ac => ac["types"].Values<string>()
                                    .Contains("administrative_area_level_1")).FirstOrDefault();
        var enLocality = enAddressComponent?.Where(ac => ac["types"].Values<string>()
                                    .Contains("locality")).FirstOrDefault();

        var countryName = country?["long_name"]?.Value<string>();
        var adminAreaName = adminArea?["long_name"]?.Value<string>();
        var localityName = locality?["long_name"]?.Value<string>();
        var enCountryName = enCountry?["long_name"]?.Value<string>();
        var enAdminAreaName = enAdminArea?["long_name"]?.Value<string>();
        var enLocalityName = enLocality?["long_name"]?.Value<string>();
        var countryCode = country?["short_name"]?.Value<string>();

        var countryRegions = string.IsNullOrEmpty(countryCode) ? [] : _countryHelper.GetCountryByCode(countryCode).Regions;
        var adminAreaCode = countryRegions?.Where(x => x.Name == enAdminAreaName)?.FirstOrDefault()?.ShortCode;

        var arrivalLocation = new ArrivalLocationData
        (
            arrivalDate: datetime,
            countryName: countryName ?? string.Empty,
            adminAreaName: adminAreaName ?? string.Empty,
            localityName: localityName ?? string.Empty,
            enCountryName: enCountryName ?? string.Empty,
            enAdminAreaName: enAdminAreaName ?? string.Empty,
            enLocalityName: enLocalityName ?? string.Empty,
            countryCode: countryCode ?? string.Empty,
            adminAreaCode: adminAreaCode ?? string.Empty,
            location: location
        );

        return arrivalLocation;
    }

    private static async Task<JObject> GetRequestForLocationInfo(Location location, string languageCode)
    {
        if (languageCode.Length != 2) throw new ArgumentException("Two-letter ISO language code must be 2 characters long");
        if (!languageCode.All(char.IsLetter)) throw new ArgumentException("Two-letter ISO language code only has letters");

        string requestUrl = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={location.Latitude},{location.Longitude}&result_type=locality&language={languageCode}&key={Constants.GoogleMapApiKey}";

        using (HttpClient httpClient = new HttpClient())
        {
            HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
            if (!response.IsSuccessStatusCode) throw new Exception("Unable to get location");

            string responseContent = await response.Content.ReadAsStringAsync();
            dynamic jsonResponse = JsonConvert.DeserializeObject(responseContent);
            if (jsonResponse is null) throw new Exception("Unable to get location");

            return JObject.Parse(responseContent);
        }
    }

    private static async Task<JObject> GetAllRequestsForLocationInfo(Location location, string languageCode)
    {
        try
        {
            if (languageCode.Length != 2) throw new ArgumentException("Two-letter ISO language code must be 2 characters long");
            if (!languageCode.All(char.IsLetter)) throw new ArgumentException("Two-letter ISO language code only has letters");

            string requestUrl = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={location.Latitude},{location.Longitude}&language={languageCode}&key={Constants.GoogleMapApiKey}";

            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                if (!response.IsSuccessStatusCode) throw new Exception("Unable to get location");

                string responseContent = await response.Content.ReadAsStringAsync();
                dynamic jsonResponse = JsonConvert.DeserializeObject(responseContent);
                if (jsonResponse is null) throw new Exception("Unable to get location");

                return JObject.Parse(responseContent);
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Unable to get location", ex);
        }
    }
}
