using System.Collections.ObjectModel;
using FlagsRally.Models;
using FlagsRally.Models.CustomBoard;
using Maui.GoogleMaps;

namespace FlagsRallyTests.Models;

/// <summary>
/// Two boards can share a place (香取神宮 is on 全国一の宮巡り and on 全国神宮二十五社巡り).
/// Maui.GoogleMaps compares pins by label, position, type and address, so the two pins were "equal"
/// until one was checked in: choosing the other one in the overlap chooser left the first one
/// selected (iOS), and Check In then recorded the wrong board's place.
/// </summary>
public class MapPinIdentityTests
{
    static CustomLocationPin Katori(string board) => new(new CustomLocationData
    {
        CompositeKey = $"{board}-katori",
        BoardName = board,
        Code = "katori",
        Title = "香取神宮",
        Latitude = 35.886011,
        Longitude = 140.528836,
    });

    [Fact]
    public void Pins_for_the_same_place_on_two_boards_are_different_pins()
    {
        var ichinomiya = Katori("全国一の宮巡り");
        var jingu = Katori("全国神宮二十五社巡り");

        Assert.False(ichinomiya.Equals(jingu));
        Assert.False(ichinomiya == jingu);
        Assert.True(ichinomiya.Equals(ichinomiya));
    }

    // The page's SelectedPin is an [ObservableProperty]; its setter ignores a value that
    // EqualityComparer<Pin?>.Default calls equal to the current one, so the card and Check In kept
    // the first pin.
    [Fact]
    public void Selecting_the_other_pin_at_the_same_place_counts_as_a_change()
    {
        var ichinomiya = Katori("全国一の宮巡り");
        var jingu = Katori("全国神宮二十五社巡り");

        Assert.False(EqualityComparer<Pin?>.Default.Equals(jingu, ichinomiya));
    }

    [Fact]
    public void Removing_one_pin_from_a_list_removes_that_pin()
    {
        var ichinomiya = Katori("全国一の宮巡り");
        var jingu = Katori("全国神宮二十五社巡り");
        var pins = new ObservableCollection<Pin> { ichinomiya, jingu };

        pins.Remove(jingu);

        Assert.Same(ichinomiya, Assert.Single(pins));
    }

    [Fact]
    public void Two_arrivals_at_the_same_place_on_the_same_day_are_different_pins()
    {
        var data = new ArrivalLocationData { Id = 1, AdminAreaName = "Chiba", Latitude = 35.886011, Longitude = 140.528836, ArrivalDate = new DateTime(2026, 10, 12, 9, 0, 0) };
        var first = new ArrivalLocationPin(data);
        var second = new ArrivalLocationPin(new ArrivalLocationData { Id = 2, AdminAreaName = "Chiba", Latitude = 35.886011, Longitude = 140.528836, ArrivalDate = new DateTime(2026, 10, 12, 15, 0, 0) });

        Assert.False(first.Equals(second));
    }
}
