using FlagsRally.Services;
using Microsoft.Maui.Devices.Sensors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlagsRally.Models;

public class SubRegion
{
    public DateTime ArrivalDate { get; set; }
    public string Name { get; init; }

    public SubRegionCode Code { get; init; }
    public string FlagSource => GetFlagSource();
    public bool HasBeenVisited => !(ArrivalDate == DateTime.MinValue);

    private string GetFlagSource()
    {
        if (Code.lower5LetterRegionCode[0..2] == "us")
        {
            return $"https://flagcdn.com/160x120/{Code.lower5LetterRegionCode}.png";
        }
        else if (Code.lower5LetterRegionCode[0..2] == "jp")
        {
            return $"Images/PrefectureEmblems/{Code.GetImageResourceString()}_emblem.png";
        }
        else
        {
            throw new ArgumentException("Unexpected country's SubRegionCode");
        }
    }
}
