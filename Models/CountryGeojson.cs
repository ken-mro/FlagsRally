using FlagsRally.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;



public class CountryGeojson
{
    public string type { get; set; }
    public Feature[] features { get; set; }

    public static async Task<Geometry> GetMultiPolygonByCountryCode(string countryCode)
    {
        //HttpClient client = new HttpClient();
        //string url = $"http://api.geonames.org/countryInfoJSON?country={countryCode}&username=mukkunj";

        //HttpResponseMessage response = await client.GetAsync(url);
        //response.EnsureSuccessStatusCode();

        //string json = await response.Content.ReadAsStringAsync();
        //var countryInfo = JsonSerializer.Deserialize<CountryInfo>(json);

        var info = System.Reflection.Assembly.GetExecutingAssembly().GetName();
        using var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream($"{info.Name}.Resources.Geojson.countries.geojson");
        using var streamReader = new StreamReader(stream!, Encoding.UTF8);
        string jsonString = streamReader.ReadToEnd();
        var countryGeojson = JsonSerializer.Deserialize<CountryGeojson>(jsonString);

        var multiPolygon = countryGeojson?.features.Where(x => x.properties.ISO_A2 == countryCode).FirstOrDefault();
        return new Geometry()
        {
            coordinates = multiPolygon.geometry.coordinates
        };
    }
}

public class Feature
{
    public string type { get; set; }
    public Properties properties { get; set; }
    public Geometry geometry { get; set; }
}

public class Properties
{
    public string ADMIN { get; set; }
    public string ISO_A3 { get; set; }
    public string ISO_A2 { get; set; }
}

public class Geometry
{
    public string type { get; set; }
    public float[][][][] coordinates { get; set; }
}
