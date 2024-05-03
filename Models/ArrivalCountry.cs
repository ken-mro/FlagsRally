using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlagsRally.Models;

public record ArrivalCountry
{
    public int Id { get; init; }
    public string Name { get; init; }
    public string ArrivalDate { get; init; }
    public string FlagSource { get; init; }
}
