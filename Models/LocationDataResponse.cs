namespace FlagsRally.Models;

public class LocationDataResponse
{
    public Summary summary { get; set; }
    public Address[] addresses { get; set; }
}

public class Summary
{
    public int queryTime { get; set; }
    public int numResults { get; set; }
}

public class Address
{
    public Address1 address { get; set; }
    public string position { get; set; }
    public Datasources dataSources { get; set; }
    public string entityType { get; set; }
    public string id { get; set; }
}

public class Address1
{
    public object[] routeNumbers { get; set; }
    public string countryCode { get; set; }
    public string countrySubdivision { get; set; }
    public string country { get; set; }
    public string countryCodeISO3 { get; set; }
    public string freeformAddress { get; set; }
    public Boundingbox boundingBox { get; set; }
    public string countrySubdivisionName { get; set; }
    public string countrySubdivisionCode { get; set; }
}

public class Boundingbox
{
    public string northEast { get; set; }
    public string southWest { get; set; }
    public string entity { get; set; }
}

public class Datasources
{
    public Geometry geometry { get; set; }
}

public class Geometry
{
    public string id { get; set; }
}
