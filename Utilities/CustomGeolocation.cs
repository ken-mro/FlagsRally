using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlagsRally.Utilities;

public static class CustomGeolocation
{
    private const string API_KEY = "YOUR_GOOGLE_MAPS_API_KEY";

    public static async Task<Placemark> GetPlacemarkAsync(Location location, string languageCode)
    {
        if (languageCode.Length != 2) throw new ArgumentException("Two-letter ISO language code must be 2 characters long");
        if (!languageCode.All(char.IsLetter)) throw new ArgumentException("Two-letter ISO language code only has letters");

        string requestUrl = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={location.Latitude},{location.Longitude}&result_type=locality&language={languageCode}&key={API_KEY}";

        using (HttpClient httpClient = new HttpClient())
        {
            HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
            if (!response.IsSuccessStatusCode) throw new Exception("Unable to get location");

            string responseContent = await response.Content.ReadAsStringAsync();
            dynamic jsonResponse = JsonConvert.DeserializeObject(responseContent);
            if (jsonResponse is null) throw new Exception("Unable to get location");

            var jsonObject = JObject.Parse(responseContent);
            var result = jsonObject["results"]?.Value<JArray>()?.FirstOrDefault();
            var addressComponent = result?["address_components"];

            var country = addressComponent?.Where(ac => ac["types"].Values<string>()
                                        .Contains("country")).FirstOrDefault();

            var adminArea = addressComponent?.Where(ac => ac["types"].Values<string>()
                                        .Contains("administrative_area_level_1")).FirstOrDefault();

            var locality = addressComponent?.Where(ac => ac["types"].Values<string>()
                                        .Contains("locality")).FirstOrDefault();

            var placemark = new Placemark
            {
                CountryName = country?["long_name"]?.Value<string>(),
                CountryCode = country?["short_name"]?.Value<string>(),
                AdminArea = adminArea?["long_name"]?.Value<string>(),
                Locality = locality?["long_name"]?.Value<string>(),
                Location = new Location(location)
            };

            return placemark;
        }
    }
}
