namespace FlagsRally.ViewModels;

/// <summary>A place's details as LocationDetailsView lists them; an empty Date leaves its row out.</summary>
public interface ILocationDetails
{
    string Date { get; }
    string CountryName { get; }
    string AdminAreaName { get; }
    string LocalityName { get; }
    string Coordinates { get; }
}
