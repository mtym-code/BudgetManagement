using BudgetManagement.Common.Database;
using BudgetManagement.Models;
using BudgetManagement.Repositories;
using BudgetManagement.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BudgetManagement.Services
{
    public class ShopExpenseBudgetService
    {
        private readonly ShopExpenseBudgetRepository _repository;

        public ShopExpenseBudgetService(ShopExpenseBudgetRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<OperationMeiItem>> GetCustomersAsync(string year, string companyCode)
        {
            using var conn = await DbConnectionFactory.CreateAndOpenAsync();
            return await _repository.GetCustomersAsync(conn, year, companyCode);
        }

        // 🌟 修正: 戻り値を Task から Task<IEnumerable<ExpenseRateItem>> に変更
        public async Task<IEnumerable<ExpenseRateItem>> OnCustomerSelectedAsync(string year, string companyCode, string selectedCustomerCode)
        {
            if (string.IsNullOrEmpty(selectedCustomerCode)) return Enumerable.Empty<ExpenseRateItem>();

            using var conn = await DbConnectionFactory.CreateAndOpenAsync();

            var expenseRates = await _repository.GetShopExpenseRatesAsync(
                conn,
                year,
                companyCode,
                selectedCustomerCode
            );

            // 🌟 dynamic のコレクションを ExpenseRateItem のリストに変換
            var ratesList = new List<ExpenseRateItem>();
            foreach (var item in expenseRates)
            {
                ratesList.Add(new ExpenseRateItem
                {
                    // ※ item.org_name や item.bumen_mei など、実際のSQLのエンティティ名に合わせてください
                    DivisionName = item.org_name ?? string.Empty,
                    Rate = (decimal)(item.keihiritu ?? 0m)
                });
            }

            if (ratesList.Any())
            {
                AdjustExpenseRateTotal(ratesList);
            }

            return ratesList;
        }

        // 🌟 修正: 引数の型を List<ExpenseRateItem> に変更
        private void AdjustExpenseRateTotal(List<ExpenseRateItem> ratesList)
        {
            decimal totalRate = ratesList.Sum(x => x.Rate);

            if (totalRate != 100m)
            {
                decimal diff = 100m - totalRate;
                ratesList[0].Rate += diff; // 最初の事業部で差分補填
            }
        }
    }
}