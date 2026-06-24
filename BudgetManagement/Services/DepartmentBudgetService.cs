using BudgetManagement.Common.Database;
using BudgetManagement.Models;
using BudgetManagement.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static BudgetManagement.Repositories.DepartmentBudgetRepository;

namespace BudgetManagement.Services
{
    public class DepartmentBudgetService
    {
        private readonly DepartmentBudgetRepository _repo;

        public DepartmentBudgetService(DepartmentBudgetRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<Company>> GetCompaniesByYearAsync(string year)
        {
            using var conn = await DbConnectionFactory.CreateAndOpenAsync();
            return await _repo.GetCompaniesByYearAsync(conn, year);
        }

        // 👇 ViewModelから呼ばれるメソッド群を追加
        public async Task<IEnumerable<SectionInfo>> GetSectionsAsync(string year, string companyCode)
        {
            using var conn = await DbConnectionFactory.CreateAndOpenAsync();
            return await _repo.GetSectionsAsync(conn, year, companyCode);
        }

        // 👇 戻り値を Task<string> から Task<bool> に変更
        public async Task<bool> GetManagementInputFlagsAsync(string year, string companyCode, string sectionCode)
        {
            using var conn = await DbConnectionFactory.CreateAndOpenAsync();
            return await _repo.GetManagementInputFlagsAsync(conn, year, companyCode, sectionCode);
        }

        // Excel出力用のデータ型は仮で object にしています。専用のモデルがあればそれに変えてください。
        public async Task<IEnumerable<MonthlyBudgetData>> GetBudgetDataForExcelAsync(string year, string companyCode, string sectionCode)
        {
            using var conn = await DbConnectionFactory.CreateAndOpenAsync();
            return await _repo.GetBudgetDataForExcelAsync(conn, year, companyCode, sectionCode);
        }

        public async Task UpsertStaffInputStatusAsync(string year, string companyCode, string sectionCode, string staffHandlerCode, string deleteFlag, string createdBy, DateTime createdAt, string updatedBy, DateTime updatedAt)
        {
            using var conn = await DbConnectionFactory.CreateAndOpenAsync();
            await _repo.UpsertStaffInputStatusAsync(conn, year, companyCode, sectionCode, staffHandlerCode, deleteFlag, createdBy, createdAt, updatedBy, updatedAt);
        }

        public async Task UpdateStaffInputStatusAsync(string staffHandlerCode, string updatedBy, DateTime updatedAt, string year, string companyCode, string sectionCode)
        {
            using var conn = await DbConnectionFactory.CreateAndOpenAsync();
            await _repo.UpdateStaffInputStatusAsync(conn, staffHandlerCode, updatedBy, updatedAt, year, companyCode, sectionCode);
        }
    }
}