using FlagsRally.Helpers;
using Newtonsoft.Json.Linq;

namespace FlagsRallyTests.Helpers;

/// <summary>
/// Responses shaped like the ones Google returned in October 2026, with a stray Hanoi address among
/// the results (first for Saint Vincent, third for San Francisco).
/// </summary>
public class GeocodeResultsTests
{
    static JObject Result(string[] types, params (string Type, string Long, string Short)[] components) => new()
    {
        ["types"] = new JArray(types),
        ["address_components"] = new JArray(components.Select(c => new JObject
        {
            ["long_name"] = c.Long,
            ["short_name"] = c.Short,
            ["types"] = new JArray(c.Type, "political"),
        })),
    };

    static readonly (string, string, string) Hanoi = ("administrative_area_level_1", "Hà Nội", "Hà Nội");
    static (string, string, string) Vietnam(string name) => ("country", name, "VN");

    static JArray SaintVincent(bool japanese)
    {
        var country = ("country", japanese ? "セントビンセントおよびグレナディーン諸島" : "Saint Vincent and the Grenadines", "VC");
        var area = ("administrative_area_level_1", "Grenadines", "06");
        var town = ("locality", japanese ? "ポート・エリザベス" : "Port Elizabeth", "Port Elizabeth");
        return
        [
            Result(["premise", "street_address"], Hanoi, Vietnam(japanese ? "ベトナム" : "Vietnam")),
            Result(["plus_code"], town, area, country),
            Result(["route"], ("locality", "Port Elizabeth", "Port Elizabeth"), area, country),
            Result(["locality", "political"], town, area, country),
            Result(["administrative_area_level_1", "political"], area, country),
            Result(["country", "political"], country),
        ];
    }

    static JArray SanFrancisco(bool japanese)
    {
        var country = ("country", japanese ? "アメリカ合衆国" : "United States", "US");
        var street = new[] { ("locality", "San Francisco", "SF"), ("administrative_area_level_1", "California", "CA"), country };
        var area = ("administrative_area_level_1", japanese ? "カリフォルニア州" : "California", "CA");
        var city = ("locality", japanese ? "サンフランシスコ" : "San Francisco", "SF");
        return
        [
            Result(["street_address"], street),
            Result(["establishment"], street),
            Result(["premise", "street_address"], Hanoi, Vietnam(japanese ? "ベトナム" : "Vietnam")),
            Result(["locality", "political"], city, area, country),
            Result(["administrative_area_level_1", "political"], area, country),
            Result(["country", "political"], country),
        ];
    }

    static string Name(JToken? component) => component?["long_name"]?.Value<string>() ?? "";

    [Fact]
    public void A_stray_result_first_does_not_change_the_country()
    {
        var country = GeocodeResults.Country(SaintVincent(japanese: false));

        Assert.Equal("VC", country?["short_name"]?.Value<string>());
        Assert.Equal("Grenadines", Name(GeocodeResults.Component(SaintVincent(false), "administrative_area_level_1", "VC")));
        Assert.Equal("Port Elizabeth", Name(GeocodeResults.Component(SaintVincent(false), "locality", "VC")));
    }

    [Fact]
    public void Japanese_names_come_from_the_place_not_the_stray_result()
    {
        Assert.Equal("セントビンセントおよびグレナディーン諸島",
            Name(GeocodeResults.Translated(SaintVincent(true), "country", "Saint Vincent and the Grenadines", "VC")));
        // Not translated by Google: kept in English rather than taken from Hanoi
        Assert.Equal("Grenadines", Name(GeocodeResults.Translated(SaintVincent(true), "administrative_area_level_1", "Grenadines", "VC")));
        Assert.Equal("ポート・エリザベス", Name(GeocodeResults.Translated(SaintVincent(true), "locality", "Port Elizabeth", "VC")));
    }

    [Fact]
    public void A_name_left_in_English_on_street_results_is_taken_from_the_area_itself()
    {
        Assert.Equal("カリフォルニア州", Name(GeocodeResults.Translated(SanFrancisco(true), "administrative_area_level_1", "California", "US")));
        Assert.Equal("サンフランシスコ", Name(GeocodeResults.Translated(SanFrancisco(true), "locality", "San Francisco", "US")));
        Assert.Equal("アメリカ合衆国", Name(GeocodeResults.Translated(SanFrancisco(true), "country", "United States", "US")));
    }

    [Fact]
    public void Without_a_result_for_the_country_itself_the_one_most_results_are_in_wins()
    {
        var results = new JArray(SanFrancisco(false).Take(3));

        Assert.Equal("US", GeocodeResults.Country(results)?["short_name"]?.Value<string>());
    }
}
