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
            using var conn = DbConnectionFactory.Create();
            return await _repo.GetAllAsync(conn);
        }

        // 【新規追加】DBに接続して認証を行う
        public async Task<bool> AuthenticateAsync(string username, string password)
        {
            using var conn = DbConnectionFactory.Create();
            var user = await _repo.GetUserByCredentialsAsync(conn, username, password);
            return user != null;
        }
    }
}