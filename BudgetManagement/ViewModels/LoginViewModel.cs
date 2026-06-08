using BudgetManagement.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

        // 【追加】UserService をDIコンテナから受け取る
        public LoginViewModel(UserService userService)
        {
            _userService = userService;
        }

        // 非同期コマンドに変更
        [RelayCommand]
        private async Task LoginAsync()
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                System.Windows.MessageBox.Show("ユーザーIDまたはパスワードを入力してください。", "入力エラー", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                // DBに接続して認証処理を実行
                var isAuthenticated = await _userService.AuthenticateAsync(Username, Password);

                if (isAuthenticated)
                {
                    // 認証成功時、メニュー画面へ遷移
                    LoginSucceeded?.Invoke();
                }
                else
                {
                    System.Windows.MessageBox.Show("ユーザーIDまたはパスワードが間違っています。", "認証失敗", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"DB接続エラー: {ex.Message}", "エラー", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}