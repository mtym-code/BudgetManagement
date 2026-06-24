using BudgetManagement.Common.Database;
using BudgetManagement.Models;
using BudgetManagement.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BudgetManagement.Services
{
    public class ExpenseNonOperatingForecastService
    {
        private readonly ExpenseNonOperatingForecastRepository _repo;

        public ExpenseNonOperatingForecastService(ExpenseNonOperatingForecastRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<SystemYearMonth>> GetSystemYearMonthsAsync(string year)
        {
            using var conn = await DbConnectionFactory.CreateAndOpenAsync();
            return await _repo.GetSystemYearMonthsAsync(conn, year);
        }

        public async Task<IEnumerable<Company>> GetCompaniesByYearAsync(string year)
        {
            using var conn = await DbConnectionFactory.CreateAndOpenAsync();
            return await _repo.GetCompaniesByYearAsync(conn, year);
        }

        public async Task<bool> GetStaffInputCompleteFlagAsync(string year, string companyCode, int targetMonth)
        {
            using var conn = await DbConnectionFactory.CreateAndOpenAsync();
            return await _repo.GetStaffInputCompleteFlagAsync(conn, year, companyCode, targetMonth);
        }

        public async Task<IEnumerable<ForecastData>> GetForecastDataAsync(string year, string companyCode, string targetMonth)
        {
            using var conn = await DbConnectionFactory.CreateAndOpenAsync();
            return await _repo.GetForecastDataAsync(conn, year, companyCode, targetMonth);
        }

        public async Task UpsertStaffInputStatusAsync(string year, string companyCode, int targetMonth, string staffHandlerCode, bool deleteFlag, string userId)
        {
            using var conn = await DbConnectionFactory.CreateAndOpenAsync();
            await _repo.UpsertStaffInputStatusAsync(conn, year, companyCode, targetMonth, staffHandlerCode, deleteFlag, userId);
        }
    }
}