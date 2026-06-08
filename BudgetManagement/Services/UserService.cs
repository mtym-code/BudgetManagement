using BudgetManagement.Models;
using BudgetManagement.Repositories;
using BudgetManagement.Common.Database;

namespace BudgetManagement.Services
{
    public class UserService
    {
        private readonly UserRepository _repo;

        public UserService(UserRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<User>> GetUsersAsync()
        {
            using var conn = DbConnectionFactory.Create();

            return await _repo.GetAllAsync(conn);
        }
    }
}