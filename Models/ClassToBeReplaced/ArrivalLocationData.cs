using SQLite;

namespace FlagsRally.Models
{
    [Table("ArrivalLocation")]
    public class ArrivalLocationData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; init; }

        public DateTime ArrivalDate { get; init; }

        [MaxLength(168)]
        public string CountryName { get; init; }

        [MaxLength(168)]
        public string AdminAreaName { get; init; }

        [MaxLength(168)]
        public string LocalityName { get; init; }

        [MaxLength(168)]
        public string EnCountryName { get; init; }

        [MaxLength(168)]
        public string EnAdminAreaName { get; init; }

        [MaxLength(168)]
        public string EnLocalityName { get; init; }

        [MaxLength(2)]
        public string CountryCode { get; init; }

        [MaxLength(5)]
        public string AdminAreaCode { get; init; }

        public double Latitude { get; init; }

        public double Longitude { get; init; }
    }
}
