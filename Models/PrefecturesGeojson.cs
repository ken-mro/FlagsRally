using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlagsRally.Models;

public class PrefecturesGeojson
{
    public string type { get; set; }
    public Metadata metadata { get; set; }
    public Feature[] features { get; set; }

    public static async Task<Geometry> GetSubRegionMultiPolygonBy(SubRegionCode subRegionCode)
    {
        try
        {
        var info = System.Reflection.Assembly.GetExecutingAssembly().GetName();
        using var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream($"{info.Name}.Resources.Geojson.prefectures.geojson");
        using var streamReader = new StreamReader(stream!, Encoding.UTF8);
        string jsonString = streamReader.ReadToEnd();
        var prefecturesGeojson = JsonSerializer.Deserialize<PrefecturesGeojson>(jsonString);

        var multiPolygon = prefecturesGeojson?.features.Where(x => x.properties.pref.ToLower() == subRegionCode.lower5LetterRegionCode).FirstOrDefault();
        return new Geometry()
        {
            coordinates = multiPolygon.geometry.coordinates
        };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new Geometry();
        }
    }

}
public class Metadata
{
    public string generated { get; set; }
    public string notice { get; set; }
}

public class Feature
{
    public string type { get; set; }
    public Properties properties { get; set; }
    public Geometry geometry { get; set; }
}

public class Properties
{
    public string pref { get; set; }
    public string name { get; set; }
}

public class Geometry
{
    public string type { get; set; }
    public float[][][][] coordinates { get; set; }
}
