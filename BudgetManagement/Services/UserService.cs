using BudgetManagement.Models;
using BudgetManagement.Repositories;
using BudgetManagement.Common.Database;
using System.Threading.Tasks;
using System.Collections.Generic;

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
            using var conn = await DbConnectionFactory.CreateAndOpenAsync();
            return await _repo.GetAllAsync(conn);
        }

        // ★修正：bool ではなく User（ユーザー情報）を返すように変更
        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            using var conn = await DbConnectionFactory.CreateAndOpenAsync();
            var user = await _repo.GetUserByCredentialsAsync(conn, username, password);
            return user;
        }
    }
}