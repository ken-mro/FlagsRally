using FlagsRally.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlagsRally.Repository
{
    public class ArrivalInfoRepository : IArrivalInfoRepository
    {
        string _dbPath = Constants.DataBasePath;

        public string StatusMessage { get; set; }

        private SQLiteAsyncConnection _conn;

        private async Task Init()
        {
            if (_conn != null)
                return;

            _conn = new SQLiteAsyncConnection(_dbPath);
            await _conn.CreateTableAsync<ArrivalInfo>();
        }

        public async Task<int> Insert(ArrivalInfo arrivalInfo)
        {

            try
            {
                await Init();
                return await _conn.InsertAsync(arrivalInfo);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to insert {arrivalInfo.Id}. Error: {ex.Message}";
                throw;
            }
        }

        public async Task<List<ArrivalInfo>> GetAll()
        {
            try
            {
                await Init();
                return await _conn.Table<ArrivalInfo>().ToListAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to retrieve data. Error: {ex.Message}";
                throw;
            }
        }

        public async Task<List<ArrivalInfo>> GetAllByCountryCode(string countryCode)
        {
            if (countryCode.Length != 2 || !countryCode.All(char.IsLetter))
            throw new ArgumentException("Country Code only has 2 letters");
            return await _conn.Table<ArrivalInfo>().Where(ai => ai.CountryCode == countryCode).ToListAsync();
        }

        public async Task<int> DeleteAsync(int Id)
        {
            return await _conn.Table<ArrivalInfo>().Where(ai => ai.Id == Id).DeleteAsync();
        }
    }
}
