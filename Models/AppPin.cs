using System.Runtime.CompilerServices;
using Maui.GoogleMaps;

namespace FlagsRally.Models;

/// <summary>
/// Base of the pins this app puts on the map. Each pin is its own place: Maui.GoogleMaps' Pin
/// compares label, position, type and address, so the same shrine on two boards (香取神宮 on
/// 全国一の宮巡り and on 全国神宮二十五社巡り) or two arrivals at one spot on the same day were
/// "equal". Choosing the other one then did not change the selected pin, and Check In recorded
/// the place still selected.
/// </summary>
public abstract class AppPin : Pin
{
    public override bool Equals(object? obj) => ReferenceEquals(this, obj);

    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}
