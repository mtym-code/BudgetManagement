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

        [ObservableProperty]
        private string username = string.Empty;
        
        [ObservableProperty]
        private string password = string.Empty;
        
        public LoginViewModel(UserService userService)
        {
            _userService = userService;
        }

        public async Task LoginAsync()
        {
            //TODO 開発用の暫定
            username = "900017";
            password = "900017";
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
                    SessionManager.UserId = Username;
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