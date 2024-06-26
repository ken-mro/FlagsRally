﻿using FlagsRally.Models;

namespace FlagsRally.Repository;

public interface IArrivalLocationDataRepository
{
    Task<int> Save(ArrivalLocationData arrivalLocationData);
    Task<List<ArrivalLocation>> GetAllArrivalLocations();
    Task<int> DeleteAsync(int Id);
    Task<List<ArrivalLocationPin>> GetArrivalLocationPinsAsync();
    Task<List<SubRegion>> GetSubRegionsByCountryCode(string countryCode);
    Task<int> UpdateAdminAreaCode(int Id, string adminAreaCode);
}