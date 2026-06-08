using BudgetManagement.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading.Tasks;

namespace BudgetManagement.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly UserService _userService;
        public event Action LoginSucceeded;

        [ObservableProperty]
        private string username;

        [ObservableProperty]
        private string password;

        public LoginViewModel(UserService userService)
        {
            _userService = userService;
        }

        // ⭕ public メソッドに変更し、Viewから直接呼べるようにしました
        public async Task LoginAsync()
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                System.Windows.MessageBox.Show("ユーザーIDまたはパスワードを入力してください。", "入力エラー", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                // DB認証を実行
                var isAuthenticated = await _userService.AuthenticateAsync(Username, Password);

                if (isAuthenticated)
                {
                    // 成功時はメニュー画面へ
                    LoginSucceeded?.Invoke();
                }
                else
                {
                    // ⭕ エラーメッセージをご指定の文言に修正しました
                    System.Windows.MessageBox.Show("ID、またはパスワードが違います", "認証失敗", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                // 万が一DBの接続文字列やネットワーク自体に問題がある場合は、こちらで検知できます
                System.Windows.MessageBox.Show($"DB接続エラーが発生しました:\n{ex.Message}", "システムエラー", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}