using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlagsRally.Models;

public record ArrivalLocation
{
    public int Id { get; init; }
    public DateTime ArrivalDate { get; init; }
    public string CountryCode { get; init; }
    public string CountryName { get; init; }
    public string CountryFlagSource { get; init; }
    public string AdminAreaName { get; init; }
    public string AdminAreaFlagSource { get; init; }
}
