using BudgetManagement.Common.Helper;
using BudgetManagement.Models;
using Dapper;
using System.Data;

namespace BudgetManagement.Repositories
{
    public class UserRepository
    {
        public async Task<IEnumerable<User>> GetAllAsync(IDbConnection conn)
        {
            var sql = SqlLoader.Load("User/GetAll.sql");

            return await conn.QueryAsync<User>(sql);
        }
    }
}