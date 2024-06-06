using FlagsRally.Models;
using FlagsRally.Repository;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        var jsonObject = await GetAllRequestsForLocationInfo(location, languageCode);
        var enJsonObject = languageCode == "en" ? jsonObject : await GetAllRequestsForLocationInfo(location, "en");

        return GenerateFrom(datetime, jsonObject, enJsonObject, location);
    }

    private ArrivalLocationData GenerateFrom(DateTime datetime, JObject jsonObject, JObject enJsonObject, Location location)
    {
        var results = jsonObject["results"]?.Value<JArray>();
        var country = GetComponent(results, "country");
        var adminArea = GetComponent(results, "administrative_area_level_1");
        var locality = GetComponent(results, "locality");

        var enResults = enJsonObject["results"]?.Value<JArray>();
        var enCountry = GetComponent(enResults, "country");
        var enAdminArea = GetComponent(enResults, "administrative_area_level_1");
        var enLocality = GetComponent(enResults, "locality");

        var countryName = country?["long_name"]?.Value<string>();
        var adminAreaName = adminArea?["long_name"]?.Value<string>();
        var localityName = locality?["long_name"]?.Value<string>();
        var enCountryName = enCountry?["long_name"]?.Value<string>();
        var enAdminAreaName = enAdminArea?["long_name"]?.Value<string>() ?? string.Empty;
        var enAdminAreaShortName = enAdminArea?["short_name"]?.Value<string>() ?? string.Empty;
        var enLocalityName = enLocality?["long_name"]?.Value<string>();
        var countryCode = country?["short_name"]?.Value<string>() ?? string.Empty;

        var adminAreaCode = _countryHelper.GetAdminAreaCode(countryCode, enAdminAreaName, enAdminAreaShortName);

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

    private JToken? GetComponent(JToken results, string type)
    {
        foreach (var result in results)
        {
            var addressComponent = result?["address_components"];
            var targetComponent = addressComponent?.Where(ac => ac["types"].Values<string>()
                                    .Contains(type)).FirstOrDefault();
            if (targetComponent != null) return targetComponent;
        }

        return null;
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
