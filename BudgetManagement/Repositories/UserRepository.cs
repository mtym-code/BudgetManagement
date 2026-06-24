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
        public UserRepository()
        {
            // =========================================================
            // ★新規追加：データベースの「operation_type(スネークケース)」を
            // C#の「OperationType(パスカルケース)」に自動で紐付けるための設定
            // =========================================================
            DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        public async Task<IEnumerable<User>> GetAllAsync(IDbConnection conn)
        {
            var sql = SqlLoader.Load("User/GetAll.sql");
            return await conn.QueryAsync<User>(sql);
        }

        public async Task<User?> GetUserByCredentialsAsync(IDbConnection conn, string username, string password)
        {
            var sql = SqlLoader.Load("User/Login.sql");

            // ⭕ 数値（1, 0）から文字列（"1", "0"）に戻します
            var parameters = new
            {
                StaffType = "03",      // 担当区分
                StaffCode = username,  // 担当者コード
                Password = password,   // パスワード
                DeleteFlag = false     // 削除フラグ
            };

            return await conn.QuerySingleOrDefaultAsync<User>(sql, parameters);
        }
    }
}