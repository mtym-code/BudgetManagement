using BudgetManagement.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace BudgetManagement.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly UserService _userService;
        public event Action? LoginSucceeded;

        // ⭕ 【新規追加】画面側にメッセージ表示を依頼するイベント
        // (タイトル, メッセージ本文, アイコンの種類) を渡します
        public event Action<string, string, MessageBoxImage>? ShowMessage;

        // TODO 開発用暫定
        [ObservableProperty]
        // private string username = string.Empty;
        private string username = "900017";

        // TODO 開発用暫定
        [ObservableProperty]
        // private string password = string.Empty;
        private string password = "900017";

        public LoginViewModel(UserService userService)
        {
            _userService = userService;
        }

        public async Task LoginAsync()
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                // ⭕ イベントを発火して画面側に処理を委譲
                ShowMessage?.Invoke("入力エラー", "ユーザーIDまたはパスワードを入力してください。", MessageBoxImage.Warning);
                return;
            }

            try
            {
                var isAuthenticated = await _userService.AuthenticateAsync(Username, Password);

                if (isAuthenticated)
                {
                    LoginSucceeded?.Invoke();
                }
                else
                {
                    // ⭕ イベントを発火
                    ShowMessage?.Invoke("認証失敗", "ID、またはパスワードが違います", MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                // ⭕ イベントを発火
                ShowMessage?.Invoke("システムエラー", $"DB接続エラーが発生しました:\n{ex.Message}", MessageBoxImage.Error);
            }
        }
    }
}