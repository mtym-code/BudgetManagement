using System;
using System.IO;
using System.Reflection;

namespace BudgetManagement.Common.Helper
{
    public static class SqlLoader
    {
        public static string Load(string relativePath)
        {
            // 呼び出し元のexe（アセンブリ）を取得
            var assembly = Assembly.GetExecutingAssembly();
            
            // プロジェクトのルート名前空間（BudgetManagement）を取得
            var rootNamespace = assembly.GetName().Name;

            // フォルダの区切り文字「/」や「\」を、埋め込みリソース用の「.」に変換する
            // 例: "User/Login.sql" -> "User.Login.sql"
            var resourcePath = relativePath.Replace("/", ".").Replace("\\", ".");

            // 最終的なリソース名（BudgetManagement.Sql.User.Login.sql）を組み立てる
            var fullResourceName = $"{rootNamespace}.Sql.{resourcePath}";

            using (var stream = assembly.GetManifestResourceStream(fullResourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException($"埋め込みリソースが見つかりません。パスを確認してください: {fullResourceName}");
                }

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}