using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlagsRally.Models;

public class ArrivalLocationPin
{
    public int Id { get; set; }
    public DateTime ArrivalDate { get; set; }
    public string ArrivalDateString => ArrivalDate.ToString("dd MMM yyyy", CultureInfo.CreateSpecificCulture("en-US"));
    public Location PinLocation { get; set; }
}
