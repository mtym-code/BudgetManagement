using BudgetManagement.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading.Tasks;
using System.Windows;
using BudgetManagement.Common; // ★追加: SessionManagerを使うため

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
                // ★修正：認証してユーザー情報（User）を受け取る
                var user = await _userService.AuthenticateAsync(Username, Password);

                if (user != null)
                {
                    // =======================================================
                    // ★追加：ログイン成功時に SessionManager に情報を記憶させる
                    // ※プロパティ名（UserId, OperationType）は実際の User モデルに合わせてください
                    // =======================================================
                    SessionManager.UserId = user.Name ?? string.Empty;
                    // ★修正：大文字から、小文字＋アンダースコアの名前に変更
                    SessionManager.OperationType = user.Role ?? string.Empty;

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