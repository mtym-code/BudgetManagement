using BudgetManagement.Common.Helper;
using Npgsql;
using System;
using System.Data;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace BudgetManagement.Common.Database
{
    public static class DbConnectionFactory
    {

        // 🌟 新しく追加：接続テストも兼ねた非同期メソッド
        public static async Task<IDbConnection> CreateAndOpenAsync()
        {
            var connStr = ConfigurationHelper.Get("ConnectionStrings:Default");
            var conn = new NpgsqlConnection(connStr);

            try
            {
                // 実際にDBに接続しにいく
                await conn.OpenAsync();
                return conn;
            }
            catch (NpgsqlException ex) when (ex.InnerException is SocketException)
            {
                // 🌟 DB未起動（通信エラー）をここで一網打尽にして、分かりやすいメッセージで投げ直す
                conn.Dispose();
                throw new Exception("データベースサーバーに接続できませんでした。\nサーバー（PostgreSQL）が起動しているか、ネットワーク設定を確認してください。");
            }
            catch (Exception)
            {
                // その他の接続エラー
                conn.Dispose();
                throw new Exception("データベースへの接続中に予期せぬエラーが発生しました。");
            }
        }
    }
}