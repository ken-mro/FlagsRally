using FlagsRally.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlagsRally.Repository
{
    public interface IArrivalInfoRepository
    {
        Task Insert(ArrivalInfo arrivalInfo);
        Task<List<ArrivalInfo>> GetAll();
        Task<List<ArrivalInfo>> GetAllByCountryCode(string countryCode);
    }
}
