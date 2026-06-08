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

        // 【修正】ハードコードされていたSQLを削除し、Login.sqlを読み込む形式に変更
        public async Task<User> GetUserByCredentialsAsync(IDbConnection conn, string username, string password)
        {
            // 1. SQLファイルの読み込み
            var sql = SqlLoader.Load("User/Login.sql");

            // 2. Dapperのパラメータに値をセット（自動でサニタイズされます）
            // ※ StaffTypeやDeleteFlagの固定値('1'や'0')は、システムの実際の仕様に合わせて変更してください。
            var parameters = new
            {
                StaffType = "1",       // {0} に該当
                StaffCode = username,  // {1} に該当
                Password = password,   // {2} に該当
                DeleteFlag = "0"       // {3} に該当
            };

            // 3. 実行して結果を返す
            return await conn.QuerySingleOrDefaultAsync<User>(sql, parameters);
        }
    }
}