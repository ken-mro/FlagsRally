using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlagsRally.Models;

public record ArrivalLocation
{
    public int Id { get; init; }
    public DateTime ArrivalDate { get; init; }
    public string ArrivalDateString => ArrivalDate.ToString("dd MMM yyyy");

    public string CountryCode { get; init; } = string.Empty;
    public string CountryName { get; init; } = string.Empty;
    public string CountryFlagSource { get; init; } = string.Empty;

    public string AdminAreaName { get; init; } = string.Empty;
    public string AdminAreaCode { get; init; } = string.Empty;
    public string AdminAreaFlagSource { get; init; } = string.Empty;

    public string LocalityName { get; init; } = string.Empty;
    
    public Location Location { get; init; } = new Location();
}
