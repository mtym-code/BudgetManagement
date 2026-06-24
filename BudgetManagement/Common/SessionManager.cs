namespace BudgetManagement.Common
{
    // アプリが起動している間、ずっとデータを保持してくれる静的クラス
    public static class SessionManager
    {
        public static string UserId { get; set; } = string.Empty;
        public static string OperationType { get; set; } = string.Empty;
        // 会社コードや名前など、他に必要なものがあればここに追加
    }
}