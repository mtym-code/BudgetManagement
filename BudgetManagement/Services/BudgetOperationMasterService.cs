using BudgetManagement.Common.Database;
using BudgetManagement.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BudgetManagement.Services
{
    public class BudgetOperationMasterService
    {
        private readonly BudgetOperationMasterRepository _repo;

        public BudgetOperationMasterService(BudgetOperationMasterRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<OperationMeiItem>> GetCompaniesAsync()
        {
            using var conn = await DbConnectionFactory.CreateAndOpenAsync();
            return await _repo.GetCompaniesAsync(conn);
        }

        public async Task<IEnumerable<OperationMeiItem>> GetDepartmentsAsync()
        {
            using var conn = await DbConnectionFactory.CreateAndOpenAsync();
            return await _repo.GetDepartmentsAsync(conn);
        }

        public async Task<IEnumerable<OperationMeiItem>> GetSectionsAsync()
        {
            using var conn = await DbConnectionFactory.CreateAndOpenAsync();
            return await _repo.GetSectionsAsync(conn);
        }

        public async Task<IEnumerable<BudgetOperationItem>> GetOperationMasterListAsync(
            string year, string inputScreen, string companyCode, string deptCode, string sectionCode, string targetMonth)
        {
            using var conn = await DbConnectionFactory.CreateAndOpenAsync();
            return await _repo.GetOperationMasterListAsync(conn, year, inputScreen, companyCode, deptCode, sectionCode, targetMonth);
        }

        public async Task UpdateOperationMasterAsync(string year, string inputScreen, IEnumerable<BudgetOperationItem> items, string userId)
        {
            using var conn = await DbConnectionFactory.CreateAndOpenAsync();
            using var tx = conn.BeginTransaction();
            try
            {
                foreach (var item in items)
                {
                    // グリッドに表示されている行をすべて一括でUPSERT
                    await _repo.UpdateOperationMasterAsync(conn, year, inputScreen, item, userId);
                }
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }
}