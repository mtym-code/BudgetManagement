using BudgetManagement.Repositories;

namespace BudgetManagement.Services
{
    public class ShopExpenseBudgetImportService
    {
        private readonly ShopExpenseBudgetRepository _repo;

        public ShopExpenseBudgetImportService(ShopExpenseBudgetRepository repo)
        {
            _repo = repo;
        }

        // ※ インポートロジックは追って追加します
    }
}