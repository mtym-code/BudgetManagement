using BudgetManagement.Common.Helper;
using BudgetManagement.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace BudgetManagement.Repositories
{
    public class UserRepository
    {
        public async Task<IEnumerable<User>> GetAllAsync(IDbConnection conn)
        {
            var sql = SqlLoader.Load("User/GetAll.sql");
            return await conn.QueryAsync<User>(sql);
        }

        public async Task<User> GetUserByCredentialsAsync(IDbConnection conn, string username, string password)
        {
            var sql = SqlLoader.Load("User/Login.sql");

            // ⭕ PostgreSQLの型エラーを防ぐため、固定値を文字列から数値（1 や 0）に変更しました
            // ※もしお使いのDBの「staff_type」が文字列型の場合は、"1" のままで大丈夫です。
            var parameters = new
            {
                StaffType = 1,         // {0} に該当（数値型に安全化）
                StaffCode = username,  // {1} に該当
                Password = password,   // {2} に該当
                DeleteFlag = 0         // {3} に該当（数値型に安全化）
            };

            return await conn.QuerySingleOrDefaultAsync<User>(sql, parameters);
        }
    }
}