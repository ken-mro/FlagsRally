namespace FlagsRally.Models;

public class LocationDataResponse
{
    public Summary summary { get; set; } = new Summary();
    public Address[] addresses { get; set; } = [];
}

public class Summary
{
    public int queryTime { get; set; }
    public int numResults { get; set; }
}

public class Address
{
    public Address1 address { get; set; } = new Address1();
    public string position { get; set; } = string.Empty;
    public Datasources dataSources { get; set; } = new Datasources();
    public string entityType { get; set; } = string.Empty;
    public string id { get; set; } = string.Empty;
}

public class Address1
{
    public object[] routeNumbers { get; set; } = [];
    public string countryCode { get; set; } = string.Empty;
    public string countrySubdivision { get; set; } = string.Empty;
    public string country { get; set; } = string.Empty;
    public string countryCodeISO3 { get; set; } = string.Empty;
    public string freeformAddress { get; set; } = string.Empty;
    public Boundingbox boundingBox { get; set; } = new Boundingbox();
    public string countrySubdivisionName { get; set; } = string.Empty;
    public string countrySubdivisionCode { get; set; } = string.Empty;
}

public class Boundingbox
{
    public string northEast { get; set; } = string.Empty;
    public string southWest { get; set; } = string.Empty;
    public string entity { get; set; } = string.Empty;
}

public class Datasources
{
    public Geometry geometry { get; set; } = new Geometry();
}

public class Geometry
{
    public string id { get; set; } = string.Empty;
}
