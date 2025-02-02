﻿using FlagsRally.Resources;
using Microsoft.Maui.Devices.Sensors;
using SQLite;

namespace FlagsRally.Models;

[Table("ArrivalLocation")]
public class ArrivalLocationData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime ArrivalDate { get; init; }

    [MaxLength(168)]
    public string CountryName { get; init; } = string.Empty;

    [MaxLength(168)]
    public string AdminAreaName { get; init; } = string.Empty;

    [MaxLength(168)]
    public string LocalityName { get; init; } = string.Empty;

    [MaxLength(168)]
    public string EnCountryName { get; init; } = string.Empty;

    [MaxLength(168)]
    public string EnAdminAreaName { get; init; } = string.Empty;

    [MaxLength(168)]
    public string EnLocalityName { get; init; } = string.Empty;

    [MaxLength(2)]
    public string CountryCode { get; init; } = string.Empty;

    [MaxLength(5)]
    public string AdminAreaCode { get; init; } = string.Empty;

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public ArrivalLocationData() { } // Required for SQLite

    public ArrivalLocationData(DateTime arrivalDate, string countryName, string adminAreaName, string localityName, string enCountryName, string enAdminAreaName, string enLocalityName, string countryCode, string adminAreaCode, Location location)
    {
        ArrivalDate = arrivalDate;
        CountryName = countryName;
        AdminAreaName = adminAreaName;
        LocalityName = localityName;
        EnCountryName = enCountryName;
        EnAdminAreaName = enAdminAreaName;
        EnLocalityName = enLocalityName;
        CountryCode = countryCode;
        AdminAreaCode = adminAreaCode;
        Latitude = location.Latitude;
        Longitude = location.Longitude;
    }

    public override string ToString()
    {
        var roundedLongitude = Math.Round(Longitude, 3);
        var roundedLatitude = Math.Round(Latitude, 3);
        return $"{AppResources.Country}: {CountryName}\n" + 
                $"{AppResources.AdminArea}: {AdminAreaName}\n" +
                $"{AppResources.Locality}: {LocalityName}\n" +
                $"{AppResources.Location}: {roundedLatitude}, {roundedLongitude}";
    }
}
