using Newtonsoft.Json;
using SQLite;

namespace FlagsRally.Models
{
    [Obsolete("This class is obsolete, use ArrivalLocationData instead")]
    [Table("ArrivalInfo")]
    public class ArrivalInfo
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; init; }

        public DateTime ArrivalDate { get; init; }

        [MaxLength(2)]
        public string CountryCode { get; init; }
        [MaxLength(100)]
        public string AdminArea { get; init; }

        [MaxLength(250), JsonProperty]
        public string Placemark { get; init; }

    }
}
